namespace RtcConformidade.Tests;

/// <summary>A raiz deste repositório, marcada pelo RtcConformidade.slnx.</summary>
public static class Repositorio
{
    public static string Raiz { get; } = AcharRaiz();

    private static string AcharRaiz()
    {
        // Sobe a partir da pasta de saída do build até achar o .slnx.
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "RtcConformidade.slnx")))
            {
                return dir.FullName;
            }
        }

        throw new DirectoryNotFoundException("Não achei a raiz do repositório (RtcConformidade.slnx).");
    }
}
