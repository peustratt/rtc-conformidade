namespace RtcConformidade.Tests;

/// <summary>
/// URLs base das calculadoras. As variáveis de ambiente permitem rodar a suíte
/// contra outro endereço ou outra versão sem mudar o código.
/// </summary>
public static class Calculadoras
{
    public static Uri RegimeRegular { get; } =
        Ler("RTC_REGIME_REGULAR_URL", "http://localhost:8080/api/calculadora/");

    public static Uri SimplesNacional { get; } =
        Ler("RTC_SIMPLES_NACIONAL_URL", "http://localhost:8082/api/");

    private static Uri Ler(string variavel, string padrao)
    {
        var valor = Environment.GetEnvironmentVariable(variavel);
        var texto = string.IsNullOrWhiteSpace(valor) ? padrao : valor.Trim();

        // Com a barra final, caminhos relativos como "regime-geral" são somados à URL base.
        return new Uri(texto.EndsWith('/') ? texto : texto + "/");
    }
}
