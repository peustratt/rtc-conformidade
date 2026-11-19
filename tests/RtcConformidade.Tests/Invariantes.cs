using System.Globalization;
using System.Text.Json;

namespace RtcConformidade.Tests;

/// <summary>
/// Regras que valem para qualquer resposta de cálculo da calculadora do regime regular,
/// sem precisar saber o valor certo de cada campo.
/// </summary>
public static class Invariantes
{
    // Campo em total.tribCalc.IBSCBSTot e o campo correspondente em tribCalc.IBSCBS.gIBSCBS de cada item.
    private static readonly (string Total, string Item)[] CamposSomados =
    [
        ("gCBS.vCBS", "gCBS.vCBS"),
        ("gIBS.vIBS", "vIBS"),
        ("gIBS.gIBSUF.vIBSUF", "gIBSUF.vIBSUF"),
        ("gIBS.gIBSMun.vIBSMun", "gIBSMun.vIBSMun"),
    ];

    /// <summary>
    /// Invariante 1: cada valor do total é a soma dos valores dos itens como aparecem na
    /// resposta, já arredondados. É o que a SEFAZ confere ao autorizar a nota: para a CBS,
    /// a rejeição 1091 ("Total de CBS difere da soma dos itens").
    /// </summary>
    public static List<string> TotalIgualASomaDosItens(JsonElement resposta)
    {
        var problemas = new List<string>();

        // Itens sem gIBSCBS (por exemplo, só CST e cClassTrib) não entram na soma.
        var grupos = new List<JsonElement>();
        if (Json.Navegar(resposta, "objetos", out var objetos) && objetos.ValueKind == JsonValueKind.Array)
        {
            foreach (var objeto in objetos.EnumerateArray())
            {
                if (Json.Navegar(objeto, "tribCalc.IBSCBS.gIBSCBS", out var grupo))
                {
                    grupos.Add(grupo);
                }
            }
        }

        if (!Json.Navegar(resposta, "total.tribCalc.IBSCBSTot", out var total))
        {
            if (grupos.Count > 0)
            {
                problemas.Add($"{grupos.Count} itens têm gIBSCBS, mas a resposta não tem total.tribCalc.IBSCBSTot");
            }

            return problemas;
        }

        foreach (var (campoTotal, campoItem) in CamposSomados)
        {
            if (!Json.Navegar(total, campoTotal, out var valorTotal))
            {
                continue;
            }

            if (!Json.TentarDecimal(valorTotal, out var declarado))
            {
                problemas.Add($"IBSCBSTot.{campoTotal} não é um número: {valorTotal.GetRawText()}");
                continue;
            }

            var soma = 0m;
            for (var i = 0; i < grupos.Count; i++)
            {
                if (!Json.Navegar(grupos[i], campoItem, out var valor))
                {
                    continue;
                }

                if (Json.TentarDecimal(valor, out var parcela))
                {
                    soma += parcela;
                }
                else
                {
                    problemas.Add($"item {i + 1}: gIBSCBS.{campoItem} não é um número: {valor.GetRawText()}");
                }
            }

            if (declarado != soma)
            {
                problemas.Add(string.Create(CultureInfo.InvariantCulture,
                    $"IBSCBSTot.{campoTotal} = {declarado}, mas a soma dos itens é {soma} (diferença {declarado - soma})"));
            }
        }

        return problemas;
    }
}
