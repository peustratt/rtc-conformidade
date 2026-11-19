namespace RtcConformidade.Tests;

/// <summary>Localiza os arquivos de caso na pasta cases/ da raiz do repositório.</summary>
public static class Casos
{
    public static string Pasta { get; } = AcharPasta();

    public static IEnumerable<string> Arquivos() =>
        Directory.EnumerateFiles(Pasta, "*.json", SearchOption.AllDirectories).Order();

    private static string AcharPasta()
    {
        // Sobe a partir da pasta de saída do build até achar a raiz, marcada pelo .slnx.
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "RtcConformidade.slnx")))
            {
                return Path.Combine(dir.FullName, "cases");
            }
        }

        throw new DirectoryNotFoundException("Não achei a raiz do repositório (RtcConformidade.slnx).");
    }
}
