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

        var problemas = Invariantes.TotalIgualASomaDosItens(resposta.RootElement);

        var problema = Assert.Single(problemas);
        Assert.Equal("IBSCBSTot.gCBS.vCBS = 27.06, mas a soma dos itens é 27.05 (diferença 0.01)", problema);
    }

    [Fact]
    public void Totais_iguais_a_soma_dos_itens_passam()
    {
        using var resposta = JsonDocument.Parse(RespostaDaIssue3("27.05"));

        Assert.Empty(Invariantes.TotalIgualASomaDosItens(resposta.RootElement));
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

        Assert.Empty(Invariantes.TotalIgualASomaDosItens(resposta.RootElement));
    }

    [Fact]
    public void Itens_com_gIBSCBS_sem_total_sao_apontados()
    {
        using var resposta = JsonDocument.Parse("""
            { "objetos": [ { "tribCalc": { "IBSCBS": { "gIBSCBS": { "gCBS": { "vCBS": "1.00" } } } } } ] }
            """);

        Assert.Single(Invariantes.TotalIgualASomaDosItens(resposta.RootElement));
    }
}
