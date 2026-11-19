using System.Text.Json;

namespace RtcConformidade.Tests;

/// <summary>Confere o formato dos arquivos em cases/, sem precisar da calculadora.</summary>
public class FormatoDosCasos
{
    private static readonly string[] CamposObrigatorios =
        ["id", "calculadora", "endpoint", "descricao", "baseLegal", "origem", "entrada", "esperado"];

    private static readonly string[] CalculadorasValidas = ["regime-regular", "simples-nacional"];

    [Fact]
    public void Todo_caso_tem_os_campos_obrigatorios_e_uma_calculadora_valida()
    {
        var problemas = new List<string>();

        foreach (var arquivo in Casos.Arquivos())
        {
            var nome = Path.GetRelativePath(Casos.Pasta, arquivo);
            using var json = JsonDocument.Parse(File.ReadAllText(arquivo));
            var caso = json.RootElement;

            foreach (var campo in CamposObrigatorios.Where(c => !caso.TryGetProperty(c, out _)))
            {
                problemas.Add($"{nome}: falta o campo \"{campo}\"");
            }

            if (caso.TryGetProperty("calculadora", out var calculadora)
                && !CalculadorasValidas.Contains(calculadora.GetString()))
            {
                problemas.Add($"{nome}: calculadora \"{calculadora}\" desconhecida");
            }
        }

        Assert.Empty(problemas);
    }

    [Fact]
    public void Ids_dos_casos_nao_se_repetem()
    {
        var repetidos = Casos.Arquivos()
            .Select(arquivo =>
            {
                using var json = JsonDocument.Parse(File.ReadAllText(arquivo));
                return json.RootElement.TryGetProperty("id", out var id) ? id.GetString() : null;
            })
            .OfType<string>()
            .GroupBy(id => id)
            .Where(grupo => grupo.Count() > 1)
            .Select(grupo => grupo.Key);

        Assert.Empty(repetidos);
    }
}
