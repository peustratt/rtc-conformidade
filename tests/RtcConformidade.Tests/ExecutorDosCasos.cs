using System.Text;
using System.Text.Json;

namespace RtcConformidade.Tests;

/// <summary>
/// Manda a entrada de cada caso em cases/ para a calculadora e confere a resposta contra o
/// esperado e as invariantes. Sobe as calculadoras em containers, se for preciso (veja Calculadoras).
/// </summary>
[Collection(ColecaoCalculadoras.Nome)]
public class ExecutorDosCasos(Calculadoras calculadoras)
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
        var nome = raiz.GetProperty("calculadora").GetString()!;
        var (metodo, caminho) = LerEndpoint(raiz.GetProperty("endpoint").GetString()!);
        var calculadora = calculadoras.Da(nome);

        Uri baseUrl;
        try
        {
            baseUrl = await calculadora.Url();
        }
        catch (Exception erro)
        {
            Assert.Fail($"Não consegui subir a calculadora {nome} em um container. "
                + $"A suíte precisa do Docker, ou de {calculadora.VariavelDaUrl} apontando para uma calculadora no ar. "
                + "Para rodar só os testes que não precisam dela: dotnet test --filter \"Requer!=calculadora\""
                + $"{Environment.NewLine}{erro.GetType().Name}: {Resumir(erro.Message)}");
            return;
        }

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
            Assert.Fail($"A calculadora não respondeu em {url} ({erro.Message})."
                + (calculadora.Externa ? $" Confira se ela está no ar no endereço de {calculadora.VariavelDaUrl}." : ""));
            return;
        }

        using (resposta)
        {
            var corpo = await resposta.Content.ReadAsStringAsync();
            Assert.True(resposta.IsSuccessStatusCode, $"HTTP {(int)resposta.StatusCode} em {metodo} {url}:{Environment.NewLine}{corpo}");

            using var json = JsonDocument.Parse(corpo);
            var problemas = Esperado.Conferir(raiz.GetProperty("esperado"), json.RootElement);
            problemas.AddRange(Invariantes.Conferir(nome, json.RootElement));

            Falhas.SeHouver($"{arquivo} não confere com {metodo} {url}", problemas);
        }
    }

    /// <summary>
    /// Um erro de build traz o log inteiro do Docker. Ficam a primeira linha e as últimas, onde
    /// está o motivo da falha (por exemplo, o erro de compilação).
    /// </summary>
    private static string Resumir(string mensagem, int ultimasLinhas = 40)
    {
        var linhas = mensagem.Split('\n');
        return linhas.Length <= ultimasLinhas + 1
            ? mensagem
            : string.Join('\n', [linhas[0], $"  [... {linhas.Length - ultimasLinhas - 1} linhas omitidas ...]", .. linhas[^ultimasLinhas..]]);
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
