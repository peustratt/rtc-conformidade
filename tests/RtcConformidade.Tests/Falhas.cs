namespace RtcConformidade.Tests;

public static class Falhas
{
    /// <summary>Falha listando um problema por linha; o Assert.Empty do xUnit corta a lista.</summary>
    public static void SeHouver(string titulo, IReadOnlyCollection<string> problemas)
    {
        if (problemas.Count > 0)
        {
            Assert.Fail($"{titulo} ({problemas.Count}):{Environment.NewLine}"
                + string.Join(Environment.NewLine, problemas.Select(p => "  - " + p)));
        }
    }
}
