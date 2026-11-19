using System.Text;
using System.Text.Json;

namespace RtcConformidade.Tests;

/// <summary>
/// Manda a entrada de cada caso em cases/ para a calculadora e confere a resposta contra o
/// esperado e as invariantes. Precisa da calculadora no ar: veja "Como rodar" no README.
/// </summary>
public class ExecutorDosCasos
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(60) };

    public static TheoryData<string> Todos()
    {
        var dados = new TheoryData<string>();
        foreach (var arquivo in Casos.Arquivos())
        {
            dados.Add(Path.GetRelativePath(Casos.Pasta, arquivo));
        }

        return dados;
    }

    [Theory]
    [MemberData(nameof(Todos))]
    [Trait("Requer", "calculadora")]
    public async Task Resposta_confere_com_o_caso(string arquivo)
    {
        using var caso = JsonDocument.Parse(File.ReadAllText(Path.Combine(Casos.Pasta, arquivo)));
        var raiz = caso.RootElement;
        var calculadora = raiz.GetProperty("calculadora").GetString();
        var (metodo, caminho) = LerEndpoint(raiz.GetProperty("endpoint").GetString()!);

        var baseUrl = calculadora == "regime-regular" ? Calculadoras.RegimeRegular : Calculadoras.SimplesNacional;
        var url = new Uri(baseUrl, caminho);

        using var requisicao = new HttpRequestMessage(metodo, url);
        if (metodo != HttpMethod.Get)
        {
            // GetRawText repassa os números como estão no arquivo, sem passar por double.
            requisicao.Content = new StringContent(raiz.GetProperty("entrada").GetRawText(), Encoding.UTF8, "application/json");
        }

        HttpResponseMessage resposta;
        try
        {
            resposta = await Http.SendAsync(requisicao);
        }
        catch (Exception erro) when (erro is HttpRequestException or TaskCanceledException)
        {
            Assert.Fail($"A calculadora não respondeu em {url} ({erro.Message}). Suba a calculadora (veja o README) "
                + "ou rode só os testes que não precisam dela: dotnet test --filter \"Requer!=calculadora\"");
            return;
        }

        using (resposta)
        {
            var corpo = await resposta.Content.ReadAsStringAsync();
            Assert.True(resposta.IsSuccessStatusCode, $"HTTP {(int)resposta.StatusCode} em {metodo} {url}:{Environment.NewLine}{corpo}");

            using var json = JsonDocument.Parse(corpo);
            var problemas = Esperado.Conferir(raiz.GetProperty("esperado"), json.RootElement);
            if (calculadora == "regime-regular")
            {
                problemas.AddRange(Invariantes.TotalIgualASomaDosItens(json.RootElement));
            }

            Falhas.SeHouver($"{arquivo} não confere com {metodo} {url}", problemas);
        }
    }

    /// <summary>Separa "POST /regime-geral" em método e caminho relativo à URL base.</summary>
    private static (HttpMethod Metodo, string Caminho) LerEndpoint(string endpoint)
    {
        var partes = endpoint.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (partes.Length != 2)
        {
            throw new FormatException($"endpoint \"{endpoint}\" precisa ser \"MÉTODO /caminho\"");
        }

        // Sem a barra inicial, o caminho é somado à URL base em vez de substituir o caminho dela.
        return (HttpMethod.Parse(partes[0]), partes[1].TrimStart('/'));
    }
}
