using System.Globalization;
using System.Text.Json;

namespace RtcConformidade.Tests;

/// <summary>
/// Regras que valem para qualquer resposta de cálculo, sem precisar saber o valor certo de cada
/// campo. Todas comparam os valores como aparecem na resposta, já arredondados: é o que a SEFAZ
/// confere ao autorizar o documento.
/// </summary>
public static class Invariantes
{
    /// <summary>Todas as invariantes da calculadora informada.</summary>
    public static List<string> Conferir(string calculadora, JsonElement resposta) => calculadora switch
    {
        "regime-regular" =>
            [.. RegimeRegular.TotalIgualASomaDosItens(resposta), .. RegimeRegular.IBSIgualASomaDeUFEMun(resposta)],
        "simples-nacional" =>
            [.. SimplesNacional.TotalIgualASomaDosItens(resposta), .. SimplesNacional.TotalIgualASomaDosTributos(resposta)],
        _ => throw new ArgumentException($"calculadora desconhecida: {calculadora}", nameof(calculadora)),
    };

    public static class RegimeRegular
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
        /// Invariante 1: cada valor do total é a soma dos valores dos itens. Para a CBS, a SEFAZ
        /// rejeita a nota com o código 1091 ("Total de CBS difere da soma dos itens").
        /// Itens sem gIBSCBS (por exemplo, só CST e cClassTrib) não entram na soma.
        /// </summary>
        public static List<string> TotalIgualASomaDosItens(JsonElement resposta) =>
            Invariantes.TotalIgualASomaDosItens(
                Itens(resposta, "tribCalc.IBSCBS.gIBSCBS"), "gIBSCBS",
                resposta, "total.tribCalc.IBSCBSTot", "IBSCBSTot", CamposSomados);

        /// <summary>
        /// Invariante 2: vIBS é a soma de vIBSUF e vIBSMun, no item e no total (regra UB54a-10 da
        /// NT 2025.002, rejeição 1150).
        /// </summary>
        public static List<string> IBSIgualASomaDeUFEMun(JsonElement resposta)
        {
            var problemas = new List<string>();
            foreach (var (indice, grupo) in Itens(resposta, "tribCalc.IBSCBS.gIBSCBS"))
            {
                SomaDasPartes($"objetos[{indice}].gIBSCBS", grupo, "vIBS", ["gIBSUF.vIBSUF", "gIBSMun.vIBSMun"], problemas);
            }

            if (Json.Navegar(resposta, "total.tribCalc.IBSCBSTot", out var total))
            {
                SomaDasPartes("IBSCBSTot", total, "gIBS.vIBS", ["gIBS.gIBSUF.vIBSUF", "gIBS.gIBSMun.vIBSMun"], problemas);
            }

            return problemas;
        }
    }

    public static class SimplesNacional
    {
        // Campo em total e o campo correspondente em tribCalc de cada item.
        private static readonly (string Total, string Item)[] CamposSomados =
        [
            ("vRBSNtot", "vRBSNitem"),
            ("vBC", "vBC"),
            ("vIBSSNtot", "vIBSSN"),
            ("vIBSSNtotPendSusp", "vIBSPendSusp"),
            ("vCBSSNtot", "vCBSSN"),
            ("vCBSSNtotPendSusp", "vCBSPendSusp"),
            ("totalIs", "IS.valor"),
            ("totalTributos", "totalItem"),
        ];

        /// <summary>
        /// Invariante 1: cada total da operação é a soma dos valores dos itens (ADR003 da
        /// calculadora; simples-nacional#4). Itens sem tribCalc não entram na soma.
        /// </summary>
        public static List<string> TotalIgualASomaDosItens(JsonElement resposta) =>
            Invariantes.TotalIgualASomaDosItens(
                Itens(resposta, "tribCalc"), "tribCalc", resposta, "total", "total", CamposSomados);

        /// <summary>
        /// Invariante 2: o total do item é a soma dos tributos devidos e do Imposto Seletivo
        /// exibidos, e o total da operação é a soma dos totais de cada tributo (ADR003 da calculadora).
        /// </summary>
        public static List<string> TotalIgualASomaDosTributos(JsonElement resposta)
        {
            var problemas = new List<string>();
            foreach (var (indice, tributos) in Itens(resposta, "tribCalc"))
            {
                SomaDasPartes($"objetos[{indice}].tribCalc", tributos, "totalItem", ["vIBSSN", "vCBSSN", "IS.valor"], problemas);
            }

            if (Json.Navegar(resposta, "total", out var total))
            {
                SomaDasPartes("total", total, "totalTributos", ["vIBSSNtot", "vCBSSNtot", "totalIs"], problemas);
            }

            return problemas;
        }
    }

    /// <summary>Os grupos achados no caminho informado dentro de cada item de "objetos", com a posição do item.</summary>
    private static List<(int Indice, JsonElement Grupo)> Itens(JsonElement resposta, string caminho)
    {
        var itens = new List<(int, JsonElement)>();
        if (Json.Navegar(resposta, "objetos", out var objetos) && objetos.ValueKind == JsonValueKind.Array)
        {
            var indice = 0;
            foreach (var objeto in objetos.EnumerateArray())
            {
                if (Json.Navegar(objeto, caminho, out var grupo))
                {
                    itens.Add((indice, grupo));
                }

                indice++;
            }
        }

        return itens;
    }

    private static List<string> TotalIgualASomaDosItens(
        List<(int Indice, JsonElement Grupo)> itens, string nomeDoGrupo,
        JsonElement resposta, string caminhoDoTotal, string nomeDoTotal,
        (string Total, string Item)[] campos)
    {
        var problemas = new List<string>();
        if (!Json.Navegar(resposta, caminhoDoTotal, out var total))
        {
            if (itens.Count > 0)
            {
                problemas.Add($"{ItensTem(itens.Count)} {nomeDoGrupo}, mas a resposta não tem {caminhoDoTotal}");
            }

            return problemas;
        }

        foreach (var (campoTotal, campoItem) in campos)
        {
            var soma = 0m;
            var parcelas = 0;
            foreach (var (indice, grupo) in itens)
            {
                if (!Json.Navegar(grupo, campoItem, out var valor))
                {
                    continue;
                }

                if (Json.TentarDecimal(valor, out var parcela))
                {
                    soma += parcela;
                    parcelas++;
                }
                else
                {
                    problemas.Add($"objetos[{indice}].{nomeDoGrupo}.{campoItem} não é um número: {valor.GetRawText()}");
                }
            }

            if (!Json.Navegar(total, campoTotal, out var valorTotal))
            {
                if (parcelas > 0)
                {
                    problemas.Add($"{nomeDoTotal}.{campoTotal} está ausente, mas {ItensTem(parcelas)} {campoItem}");
                }

                continue;
            }

            if (!Json.TentarDecimal(valorTotal, out var declarado))
            {
                problemas.Add($"{nomeDoTotal}.{campoTotal} não é um número: {valorTotal.GetRawText()}");
            }
            else if (declarado != soma)
            {
                problemas.Add(string.Create(CultureInfo.InvariantCulture,
                    $"{nomeDoTotal}.{campoTotal} = {declarado}, mas a soma dos itens é {soma} (diferença {declarado - soma})"));
            }
        }

        return problemas;
    }

    private static string ItensTem(int quantidade) => quantidade == 1 ? "1 item tem" : $"{quantidade} itens têm";

    /// <summary>
    /// Confere se o campo composto é a soma das partes presentes no mesmo grupo. Sem o campo
    /// composto, ou sem nenhuma das partes, não há o que conferir.
    /// </summary>
    private static void SomaDasPartes(string onde, JsonElement grupo, string composto, string[] partes, List<string> problemas)
    {
        if (!Json.Navegar(grupo, composto, out var valorComposto))
        {
            return;
        }

        var nomes = new List<string>();
        var valores = new List<decimal>();
        foreach (var parte in partes)
        {
            if (!Json.Navegar(grupo, parte, out var valor))
            {
                continue;
            }

            if (!Json.TentarDecimal(valor, out var numero))
            {
                problemas.Add($"{onde}.{parte} não é um número: {valor.GetRawText()}");
                return;
            }

            nomes.Add(parte);
            valores.Add(numero);
        }

        if (valores.Count == 0)
        {
            return;
        }

        if (!Json.TentarDecimal(valorComposto, out var declarado))
        {
            problemas.Add($"{onde}.{composto} não é um número: {valorComposto.GetRawText()}");
            return;
        }

        var soma = valores.Sum();
        if (declarado != soma)
        {
            // Com uma parte só, "vCBSSNtot = 11.61"; com mais, "vIBSSN + vCBSSN = 0.00 + 0.00 = 0.00".
            var conta = valores.Count == 1
                ? string.Create(CultureInfo.InvariantCulture, $"{nomes[0]} = {soma}")
                : string.Create(CultureInfo.InvariantCulture,
                    $"{string.Join(" + ", nomes)} = {string.Join(" + ", valores.Select(v => v.ToString(CultureInfo.InvariantCulture)))} = {soma}");
            problemas.Add(string.Create(CultureInfo.InvariantCulture,
                $"{onde}.{composto} = {declarado}, mas {conta} (diferença {declarado - soma})"));
        }
    }
}
