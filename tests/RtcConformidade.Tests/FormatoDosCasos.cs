using System.Text.Json;
using System.Text.RegularExpressions;

namespace RtcConformidade.Tests;

/// <summary>Confere o formato dos arquivos em cases/, sem precisar da calculadora.</summary>
public partial class FormatoDosCasos
{
    private static readonly string[] CamposObrigatorios =
        ["id", "calculadora", "endpoint", "descricao", "baseLegal", "origem", "entrada", "esperado"];

    private static readonly string[] CalculadorasValidas = ["regime-regular", "simples-nacional"];

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex FormatoDoId();

    [Fact]
    public void Todo_caso_tem_os_campos_obrigatorios_e_uma_calculadora_valida()
    {
        var problemas = new List<string>();

        foreach (var arquivo in Casos.Arquivos())
        {
            var nome = Path.GetRelativePath(Casos.Pasta, arquivo);

            JsonDocument json;
            try
            {
                json = JsonDocument.Parse(File.ReadAllText(arquivo));
            }
            catch (JsonException erro)
            {
                problemas.Add($"{nome}: JSON inválido ({erro.Message})");
                continue;
            }

            using (json)
            {
                var caso = json.RootElement;
                if (caso.ValueKind != JsonValueKind.Object)
                {
                    problemas.Add($"{nome}: o caso precisa ser um objeto JSON");
                    continue;
                }

                foreach (var campo in CamposObrigatorios.Where(c => !caso.TryGetProperty(c, out _)))
                {
                    problemas.Add($"{nome}: falta o campo \"{campo}\"");
                }

                if (caso.TryGetProperty("id", out var id)
                    && (id.ValueKind != JsonValueKind.String || !FormatoDoId().IsMatch(id.GetString()!)))
                {
                    problemas.Add($"{nome}: id {id.GetRawText()} precisa ser texto em minúsculas e com hífens");
                }

                if (caso.TryGetProperty("calculadora", out var calculadora))
                {
                    var valor = calculadora.ValueKind == JsonValueKind.String ? calculadora.GetString() : null;
                    // A primeira pasta abaixo de cases/ diz a calculadora; subpastas são permitidas.
                    var pasta = nome.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                        is [var primeira, _, ..] ? primeira : "";

                    if (!CalculadorasValidas.Contains(valor))
                    {
                        problemas.Add($"{nome}: calculadora {calculadora.GetRawText()} desconhecida");
                    }
                    else if (pasta != valor)
                    {
                        problemas.Add(pasta == ""
                            ? $"{nome}: mova o caso para a pasta \"{valor}/\""
                            : $"{nome}: o caso é da calculadora \"{valor}\", mas está na pasta \"{pasta}/\"");
                    }
                }
            }
        }

        Falhas.SeHouver("Casos com problema de formato", problemas);
    }

    [Fact]
    public void Ids_dos_casos_nao_se_repetem()
    {
        var repetidos = Casos.Arquivos()
            .Select(LerId)
            .OfType<string>()
            .GroupBy(id => id)
            .Where(grupo => grupo.Count() > 1)
            .Select(grupo => grupo.Key)
            .ToList();

        Falhas.SeHouver("Ids repetidos", repetidos);
    }

    /// <summary>Lê o id do caso. Arquivos malformados ficam de fora: o outro teste já os aponta.</summary>
    private static string? LerId(string arquivo)
    {
        try
        {
            using var json = JsonDocument.Parse(File.ReadAllText(arquivo));
            return json.RootElement.ValueKind == JsonValueKind.Object
                && json.RootElement.TryGetProperty("id", out var id)
                && id.ValueKind == JsonValueKind.String
                    ? id.GetString()
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
