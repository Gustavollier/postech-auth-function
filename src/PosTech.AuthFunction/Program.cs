using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PosTech.AuthFunction.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureLogging(logging =>
    {
        // Logs em JSON para o Datadog parsear sem regra custom.
        logging.AddJsonConsole(options =>
        {
            options.IncludeScopes = true;
            options.UseUtcTimestamp = true;
        });
    })
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        var connectionString = configuration["SqlConnectionString"]
            ?? throw new InvalidOperationException("SqlConnectionString não configurada.");

        services.AddSingleton<IClienteRepository>(_ => new ClienteRepository(connectionString));

        services.AddSingleton<ITokenService>(_ => new TokenService(
            secretKey: Ler(configuration, "Jwt__SecretKey", "Jwt:SecretKey")
                ?? throw new InvalidOperationException("Jwt:SecretKey não configurado."),
            issuer: Ler(configuration, "Jwt__Issuer", "Jwt:Issuer") ?? "PosTechChallenge",
            audience: Ler(configuration, "Jwt__Audience", "Jwt:Audience") ?? "PosTechChallenge-API",
            expirationMinutes: int.TryParse(
                Ler(configuration, "Jwt__ExpMinutes", "Jwt:ExpMinutes"), out var minutos)
                ? minutos
                : 15));
    })
    .Build();

host.Run();

// App Settings do Azure chegam como "Jwt__SecretKey"; localmente o provider de configuração
// também expõe a forma "Jwt:SecretKey". Aceitamos as duas para o mesmo código rodar nos dois lugares.
static string? Ler(Microsoft.Extensions.Configuration.IConfiguration configuration, params string[] chaves)
    => chaves.Select(chave => configuration[chave])
             .FirstOrDefault(valor => string.IsNullOrWhiteSpace(valor) is false);
