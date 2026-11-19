using System.Text.Json;

namespace RtcConformidade.Tests;

/// <summary>Confere a comparação entre o campo "esperado" dos casos e a resposta.</summary>
public class EsperadoSemCalculadora
{
    private const string Resposta = """
        {
          "objetos": [ { "nObj": 1, "vCBS": "20.84", "CST": "000" }, { "nObj": 2, "vCBS": "6.21" } ],
          "total": { "vCBS": "27.06", "simulado": true }
        }
        """;

    private static List<string> Conferir(string esperado)
    {
        using var e = JsonDocument.Parse(esperado);
        using var r = JsonDocument.Parse(Resposta);
        return Esperado.Conferir(e.RootElement, r.RootElement);
    }

    [Fact]
    public void Campos_que_o_esperado_nao_traz_sao_ignorados()
    {
        Assert.Empty(Conferir("""{ "objetos": [ { "vCBS": "20.84" } ], "total": { "simulado": true } }"""));
    }

    [Fact]
    public void Texto_diferente_e_apontado_com_o_caminho()
    {
        var problema = Assert.Single(Conferir("""{ "total": { "vCBS": "27.05" } }"""));
        Assert.Equal("$.total.vCBS: esperado \"27.05\", veio \"27.06\"", problema);
    }

    [Fact]
    public void Numero_compara_como_decimal_com_texto_numerico()
    {
        Assert.Empty(Conferir("""{ "objetos": [ { "vCBS": 20.840 }, { "vCBS": 6.21 } ] }"""));
        Assert.Single(Conferir("""{ "total": { "vCBS": 27.05 } }"""));
    }

    [Fact]
    public void Texto_com_zeros_a_esquerda_nao_vira_numero()
    {
        Assert.Single(Conferir("""{ "objetos": [ { "CST": "0" } ] }"""));
    }

    [Fact]
    public void Campo_ausente_e_lista_curta_sao_apontados()
    {
        var problemas = Conferir("""{ "total": { "vIBS": "3.01" }, "objetos": [ {}, {}, {} ] }""");

        Assert.Equal(
            new[] { "$.total.vIBS: ausente na resposta", "$.objetos[2]: a resposta só tem 2 elementos" },
            problemas);
    }
}
