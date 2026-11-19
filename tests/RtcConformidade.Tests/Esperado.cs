using System.Text.Json;

namespace RtcConformidade.Tests;

/// <summary>Confere o campo "esperado" de um caso contra a resposta da calculadora.</summary>
public static class Esperado
{
    /// <summary>
    /// O esperado é um recorte da resposta: só os campos que ele traz são conferidos.
    /// Objetos comparam campo a campo, listas comparam pela posição, texto compara exato
    /// e número compara como decimal (27.05 confere com "27.05" e com "27.050").
    /// </summary>
    public static List<string> Conferir(JsonElement esperado, JsonElement resposta)
    {
        var problemas = new List<string>();
        Conferir(esperado, resposta, "$", problemas);
        return problemas;
    }

    private static void Conferir(JsonElement esperado, JsonElement resposta, string caminho, List<string> problemas)
    {
        switch (esperado.ValueKind)
        {
            case JsonValueKind.Object when resposta.ValueKind != JsonValueKind.Object:
            case JsonValueKind.Array when resposta.ValueKind != JsonValueKind.Array:
                problemas.Add($"{caminho}: esperado {esperado.ValueKind}, veio {resposta.GetRawText()}");
                break;

            case JsonValueKind.Object:
                foreach (var campo in esperado.EnumerateObject())
                {
                    var caminhoDoCampo = $"{caminho}.{campo.Name}";
                    if (resposta.TryGetProperty(campo.Name, out var valor))
                    {
                        Conferir(campo.Value, valor, caminhoDoCampo, problemas);
                    }
                    else
                    {
                        problemas.Add($"{caminhoDoCampo}: ausente na resposta");
                    }
                }

                break;

            case JsonValueKind.Array:
                var recebidos = resposta.EnumerateArray().ToList();
                var indice = 0;
                foreach (var item in esperado.EnumerateArray())
                {
                    if (indice < recebidos.Count)
                    {
                        Conferir(item, recebidos[indice], $"{caminho}[{indice}]", problemas);
                    }
                    else
                    {
                        problemas.Add($"{caminho}[{indice}]: a resposta só tem {recebidos.Count} elementos");
                    }

                    indice++;
                }

                break;

            case JsonValueKind.Number:
                if (!Json.TentarDecimal(resposta, out var numero) || numero != esperado.GetDecimal())
                {
                    problemas.Add($"{caminho}: esperado {esperado.GetRawText()}, veio {resposta.GetRawText()}");
                }

                break;

            case JsonValueKind.String:
                if (resposta.ValueKind != JsonValueKind.String || resposta.GetString() != esperado.GetString())
                {
                    problemas.Add($"{caminho}: esperado {esperado.GetRawText()}, veio {resposta.GetRawText()}");
                }

                break;

            default: // true, false e null
                if (resposta.ValueKind != esperado.ValueKind)
                {
                    problemas.Add($"{caminho}: esperado {esperado.GetRawText()}, veio {resposta.GetRawText()}");
                }

                break;
        }
    }
}
