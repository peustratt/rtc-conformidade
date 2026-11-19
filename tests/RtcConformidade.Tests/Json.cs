using System.Globalization;
using System.Text.Json;

namespace RtcConformidade.Tests;

public static class Json
{
    /// <summary>Segue um caminho como "total.tribCalc.IBSCBSTot" por objetos aninhados.</summary>
    public static bool Navegar(JsonElement origem, string caminho, out JsonElement destino)
    {
        destino = origem;
        foreach (var parte in caminho.Split('.'))
        {
            if (destino.ValueKind != JsonValueKind.Object || !destino.TryGetProperty(parte, out destino))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Lê um número ou um texto numérico como decimal. A calculadora devolve valores monetários
    /// como texto ("27.05"), e o decimal evita o erro de arredondamento que o double traria.
    /// </summary>
    public static bool TentarDecimal(JsonElement valor, out decimal resultado)
    {
        resultado = 0;
        return valor.ValueKind switch
        {
            JsonValueKind.Number => valor.TryGetDecimal(out resultado),
            JsonValueKind.String => decimal.TryParse(
                valor.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out resultado),
            _ => false,
        };
    }
}
