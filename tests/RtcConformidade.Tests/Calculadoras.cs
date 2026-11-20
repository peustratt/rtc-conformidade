using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace RtcConformidade.Tests;

/// <summary>
/// As calculadoras oficiais, compartilhadas pelos testes da coleção "Calculadoras". Cada uma só
/// sobe quando um teste pede o endereço dela, e sobe uma vez por execução.
/// </summary>
public sealed class Calculadoras(IMessageSink mensagens) : IAsyncLifetime
{
    public Calculadora RegimeRegular { get; } = new(
        "regime-regular", "RTC_REGIME_REGULAR", portaApi: 8080, portaSaude: 9101, caminho: "/api/calculadora/", mensagens);

    public Calculadora SimplesNacional { get; } = new(
        "simples-nacional", "RTC_SIMPLES_NACIONAL", portaApi: 8082, portaSaude: 9103, caminho: "/api/", mensagens);

    public Calculadora Da(string nome) => nome switch
    {
        "regime-regular" => RegimeRegular,
        "simples-nacional" => SimplesNacional,
        _ => throw new ArgumentException($"calculadora desconhecida: {nome}", nameof(nome)),
    };

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await RegimeRegular.DisposeAsync();
        await SimplesNacional.DisposeAsync();
    }
}

[CollectionDefinition(Nome)]
public sealed class ColecaoCalculadoras : ICollectionFixture<Calculadoras>
{
    public const string Nome = "Calculadoras";
}

/// <summary>
/// Uma calculadora oficial. Com {prefixo}_URL definida, a suíte usa a calculadora que já está
/// nesse endereço. Sem ela, constrói a imagem de calculadoras/Dockerfile e sobe um container
/// (precisa do Docker). {prefixo}_REF, {prefixo}_REPO e {prefixo}_JAVA escolhem a versão, o
/// repositório e o Java da compilação.
/// </summary>
public sealed class Calculadora : IAsyncDisposable
{
    // Variável de ambiente (sem o prefixo) e o argumento de build correspondente no Dockerfile.
    // As duas calculadoras recebem os argumentos do regime regular, de onde vem o banco.
    private static readonly (string Variavel, string Argumento)[] ArgumentosDeBuild =
    [
        ("RTC_REGIME_REGULAR_REF", "RR_REF"),
        ("RTC_REGIME_REGULAR_REPO", "RR_REPO"),
        ("RTC_REGIME_REGULAR_JAVA", "RR_JAVA"),
        ("RTC_SIMPLES_NACIONAL_REF", "SN_REF"),
        ("RTC_SIMPLES_NACIONAL_REPO", "SN_REPO"),
        ("RTC_SIMPLES_NACIONAL_JAVA", "SN_JAVA"),
    ];

    // Imagem com o Docker CLI e o Buildx, em que o Testcontainers roda o build.
    private const string ImagemDoBuild = "docker:29-cli";

    private readonly string _nome;
    private readonly string _prefixo;
    private readonly int _portaApi;
    private readonly int _portaSaude;
    private readonly string _caminho;
    private readonly IMessageSink _mensagens;
    private readonly Lazy<Task<Uri>> _url;
    private IContainer? _container;

    public Calculadora(string nome, string prefixo, int portaApi, int portaSaude, string caminho, IMessageSink mensagens)
    {
        _nome = nome;
        _prefixo = prefixo;
        _portaApi = portaApi;
        _portaSaude = portaSaude;
        _caminho = caminho;
        _mensagens = mensagens;
        _url = new Lazy<Task<Uri>>(Subir);
    }

    /// <summary>A URL base da calculadora, terminada em "/". Sobe o container na primeira chamada.</summary>
    public Task<Uri> Url() => _url.Value;

    /// <summary>Se a URL veio da variável de ambiente, e não de um container desta execução.</summary>
    public bool Externa => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(_prefixo + "_URL"));

    public string VariavelDaUrl => _prefixo + "_URL";

    private async Task<Uri> Subir()
    {
        if (Externa)
        {
            var texto = Environment.GetEnvironmentVariable(VariavelDaUrl)!.Trim();
            // Com a barra final, caminhos relativos como "regime-geral" são somados à URL base.
            return new Uri(texto.EndsWith('/') ? texto : texto + "/");
        }

        // BuildKit, e não a API do Docker Engine: um build que falha vira erro mesmo que já exista
        // uma imagem com o mesmo nome, que a API reaproveitaria em silêncio. O cache do BuildKit não
        // depende da imagem, e a próxima execução só refaz o que mudou.
        var builder = new BuildKitImageFromDockerfileBuilder(ImagemDoBuild)
            .WithDockerfileDirectory(Path.Combine(Repositorio.Raiz, "calculadoras"))
            .WithTarget(_nome)
            .WithName($"rtc-conformidade/{_nome}:local")
            .WithCleanUp(false);

        var versoes = new List<string>();
        foreach (var (variavel, argumento) in ArgumentosDeBuild)
        {
            var valor = Environment.GetEnvironmentVariable(variavel);
            if (!string.IsNullOrWhiteSpace(valor))
            {
                builder = builder.WithBuildArgument(argumento, valor.Trim());
                versoes.Add($"{argumento}={valor.Trim()}");
            }
        }

        var imagem = builder.Build();
        Avisar($"construindo a imagem ({(versoes.Count > 0 ? string.Join(", ", versoes) : "versões padrão do Dockerfile")})");
        await imagem.CreateAsync();

        _container = new ContainerBuilder(imagem)
            .WithPortBinding(_portaApi, assignRandomHostPort: true)
            .WithPortBinding(_portaSaude, assignRandomHostPort: true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(
                requisicao => requisicao.ForPort((ushort)_portaSaude).ForPath("/health"),
                espera => espera.WithTimeout(TimeSpan.FromMinutes(3))))
            .Build();

        Avisar("subindo o container");
        await _container.StartAsync();

        var url = new UriBuilder("http", _container.Hostname, _container.GetMappedPublicPort(_portaApi), _caminho).Uri;
        Avisar($"no ar em {url}");
        return url;
    }

    private void Avisar(string texto) => _mensagens.OnMessage(new DiagnosticMessage($"Calculadora {_nome}: {texto}"));

    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}
