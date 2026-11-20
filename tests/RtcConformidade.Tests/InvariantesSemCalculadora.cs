using System.Text.Json;

namespace RtcConformidade.Tests;

/// <summary>Confere a própria checagem das invariantes, com respostas escritas à mão.</summary>
public class InvariantesSemCalculadora
{
    /// <summary>Recorte da resposta registrada na regime-regular#3, com o total de CBS informado.</summary>
    private static string RespostaDaIssue3(string vCBSTotal) => $$"""
        {
          "objetos": [
            { "nObj": 1, "tribCalc": { "IBSCBS": { "gIBSCBS": {
              "vBC": "2315.79",
              "gIBSUF": { "pIBSUF": "0.10", "vIBSUF": "2.32" },
              "gIBSMun": { "pIBSMun": "0.00", "vIBSMun": "0.00" },
              "vIBS": "2.32",
              "gCBS": { "pCBS": "0.90", "vCBS": "20.84" } } } } },
            { "nObj": 2, "tribCalc": { "IBSCBS": { "gIBSCBS": {
              "vBC": "690.52",
              "gIBSUF": { "pIBSUF": "0.10", "vIBSUF": "0.69" },
              "gIBSMun": { "pIBSMun": "0.00", "vIBSMun": "0.00" },
              "vIBS": "0.69",
              "gCBS": { "pCBS": "0.90", "vCBS": "6.21" } } } } }
          ],
          "total": { "tribCalc": { "IBSCBSTot": {
            "vBCIBSCBS": "3006.31",
            "gIBS": { "gIBSUF": { "vIBSUF": "3.01" }, "gIBSMun": { "vIBSMun": "0.00" }, "vIBS": "3.01" },
            "gCBS": { "vCBS": "{{vCBSTotal}}" } } } }
        }
        """;

    [Fact]
    public void Total_de_CBS_diferente_da_soma_dos_itens_e_apontado()
    {
        using var resposta = JsonDocument.Parse(RespostaDaIssue3("27.06"));

        var problemas = Invariantes.RegimeRegular.TotalIgualASomaDosItens(resposta.RootElement);

        var problema = Assert.Single(problemas);
        Assert.Equal("IBSCBSTot.gCBS.vCBS = 27.06, mas a soma dos itens é 27.05 (diferença 0.01)", problema);
    }

    [Fact]
    public void Totais_iguais_a_soma_dos_itens_passam()
    {
        using var resposta = JsonDocument.Parse(RespostaDaIssue3("27.05"));

        Assert.Empty(Invariantes.RegimeRegular.TotalIgualASomaDosItens(resposta.RootElement));
    }

    [Fact]
    public void Itens_sem_gIBSCBS_ficam_fora_da_soma()
    {
        using var resposta = JsonDocument.Parse("""
            {
              "objetos": [
                { "nObj": 1, "tribCalc": { "IBSCBS": { "gIBSCBS": { "gCBS": { "vCBS": "1.00" } } } } },
                { "nObj": 2, "tribCalc": { "IBSCBS": { "CST": "410", "cClassTrib": "410001" } } }
              ],
              "total": { "tribCalc": { "IBSCBSTot": { "gCBS": { "vCBS": "1.00" } } } }
            }
            """);

        Assert.Empty(Invariantes.RegimeRegular.TotalIgualASomaDosItens(resposta.RootElement));
    }

    [Fact]
    public void Itens_com_gIBSCBS_sem_total_sao_apontados()
    {
        using var resposta = JsonDocument.Parse("""
            { "objetos": [ { "tribCalc": { "IBSCBS": { "gIBSCBS": { "gCBS": { "vCBS": "1.00" } } } } } ] }
            """);

        Assert.Single(Invariantes.RegimeRegular.TotalIgualASomaDosItens(resposta.RootElement));
    }

    /// <summary>Recorte da resposta registrada na regime-regular#10, com o vIBS informado no item e no total.</summary>
    private static string RespostaDaIssue10(string vIBS) => $$"""
        {
          "objetos": [ { "nObj": 1, "tribCalc": { "IBSCBS": { "gIBSCBS": {
            "vBC": "11752.50",
            "gIBSUF": { "pIBSUF": "14.70", "vIBSUF": "1727.62" },
            "gIBSMun": { "pIBSMun": "3.00", "vIBSMun": "352.58" },
            "vIBS": "{{vIBS}}" } } } } ],
          "total": { "tribCalc": { "IBSCBSTot": {
            "gIBS": { "gIBSUF": { "vIBSUF": "1727.62" }, "gIBSMun": { "vIBSMun": "352.58" }, "vIBS": "{{vIBS}}" } } } }
        }
        """;

    [Fact]
    public void VIBS_diferente_de_UF_mais_Mun_e_apontado_no_item_e_no_total()
    {
        using var resposta = JsonDocument.Parse(RespostaDaIssue10("2080.19"));

        Assert.Equal(
            new[]
            {
                "objetos[0].gIBSCBS.vIBS = 2080.19, mas gIBSUF.vIBSUF + gIBSMun.vIBSMun = 1727.62 + 352.58 = 2080.20 (diferença -0.01)",
                "IBSCBSTot.gIBS.vIBS = 2080.19, mas gIBS.gIBSUF.vIBSUF + gIBS.gIBSMun.vIBSMun = 1727.62 + 352.58 = 2080.20 (diferença -0.01)",
            },
            Invariantes.RegimeRegular.IBSIgualASomaDeUFEMun(resposta.RootElement));
    }

    [Fact]
    public void VIBS_igual_a_UF_mais_Mun_passa()
    {
        using var resposta = JsonDocument.Parse(RespostaDaIssue10("2080.20"));

        Assert.Empty(Invariantes.Conferir("regime-regular", resposta.RootElement));
    }

    /// <summary>
    /// Item do Simples Nacional como a simples-nacional#4 descreve: IBS e CBS de 0,004 aparecem
    /// como 0.00, mas o total do item e o total da operação saem 0.01.
    /// </summary>
    private const string RespostaDaIssueSimples4 = """
        {
          "objetos": [ { "nObj": 1, "estadoItem": "CALCULADO", "tribCalc": {
            "vRBSNitem": "100.00", "vBC": "100.00", "vIBSSN": "0.00", "vCBSSN": "0.00", "totalItem": "0.01" } } ],
          "total": { "vRBSNtot": "100.00", "vBC": "100.00", "vIBSSNtot": "0.00", "vCBSSNtot": "0.00", "totalTributos": "0.01" }
        }
        """;

    [Fact]
    public void Simples_total_do_item_diferente_da_soma_dos_tributos_e_apontado()
    {
        using var resposta = JsonDocument.Parse(RespostaDaIssueSimples4);

        Assert.Equal(
            new[]
            {
                "objetos[0].tribCalc.totalItem = 0.01, mas vIBSSN + vCBSSN = 0.00 + 0.00 = 0.00 (diferença 0.01)",
                "total.totalTributos = 0.01, mas vIBSSNtot + vCBSSNtot = 0.00 + 0.00 = 0.00 (diferença 0.01)",
            },
            Invariantes.Conferir("simples-nacional", resposta.RootElement));
    }

    [Fact]
    public void Simples_total_diferente_da_soma_dos_itens_e_apontado()
    {
        using var resposta = JsonDocument.Parse("""
            {
              "objetos": [
                { "nObj": 1, "tribCalc": { "vCBSSN": "0.39", "IS": { "valor": "0.00" }, "totalItem": "0.39" } },
                { "nObj": 2, "tribCalc": { "vCBSSN": "0.39", "IS": { "valor": "0.00" }, "totalItem": "0.39" } },
                { "nObj": 3, "estadoItem": "INCONSISTENCIA_ENTRADA" }
              ],
              "total": { "vCBSSNtot": "0.79", "totalIs": "0.00", "totalTributos": "0.79" }
            }
            """);

        Assert.Equal(
            new[]
            {
                "total.vCBSSNtot = 0.79, mas a soma dos itens é 0.78 (diferença 0.01)",
                "total.totalTributos = 0.79, mas a soma dos itens é 0.78 (diferença 0.01)",
            },
            Invariantes.SimplesNacional.TotalIgualASomaDosItens(resposta.RootElement));
    }

    [Fact]
    public void Simples_total_sem_campo_que_os_itens_tem_e_apontado()
    {
        using var resposta = JsonDocument.Parse("""
            {
              "objetos": [ { "nObj": 1, "tribCalc": { "vIBSSN": "0.13", "vCBSSN": "11.61", "totalItem": "11.74" } } ],
              "total": { "vCBSSNtot": "11.61", "totalTributos": "11.74" }
            }
            """);

        // Sem o vIBSSNtot, o totalTributos também deixa de fechar com os totais presentes.
        Assert.Equal(
            new[]
            {
                "total.vIBSSNtot está ausente, mas 1 item tem vIBSSN",
                "total.totalTributos = 11.74, mas vCBSSNtot = 11.61 (diferença 0.13)",
            },
            Invariantes.Conferir("simples-nacional", resposta.RootElement));
    }
}
