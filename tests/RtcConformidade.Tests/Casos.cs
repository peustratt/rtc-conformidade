namespace RtcConformidade.Tests;

/// <summary>Localiza os arquivos de caso na pasta cases/ da raiz do repositório.</summary>
public static class Casos
{
    public static string Pasta { get; } = Path.Combine(Repositorio.Raiz, "cases");

    public static IEnumerable<string> Arquivos() =>
        Directory.EnumerateFiles(Pasta, "*.json", SearchOption.AllDirectories).Order();
}
