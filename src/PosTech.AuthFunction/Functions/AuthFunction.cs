using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using PosTech.AuthFunction.Domain;
using PosTech.AuthFunction.Models;
using PosTech.AuthFunction.Services;

namespace PosTech.AuthFunction.Functions;

/// <summary>
/// POST /api/auth — autenticação do cliente por CPF.
///
/// Valida o CPF, confirma que o cliente existe e está ativo na base, e devolve um JWT
/// aceito pelas rotas protegidas da API principal.
/// </summary>
public sealed class AuthFunction
{
    public const string CorrelationIdHeader = "X-Correlation-ID";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IClienteRepository _clienteRepository;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthFunction> _logger;

    public AuthFunction(
        IClienteRepository clienteRepository,
        ITokenService tokenService,
        ILogger<AuthFunction> logger)
    {
        _clienteRepository = clienteRepository;
        _tokenService = tokenService;
        _logger = logger;
    }

    [Function("Auth")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth")] HttpRequestData request,
        CancellationToken cancellationToken)
    {
        var correlationId = ObterCorrelationId(request);

        using var escopo = _logger.BeginScope(new Dictionary<string, object>
        {
            ["correlationId"] = correlationId
        });

        AuthRequest? corpo;
        try
        {
            corpo = await JsonSerializer.DeserializeAsync<AuthRequest>(
                request.Body, JsonOptions, cancellationToken);
        }
        catch (JsonException)
        {
            _logger.LogWarning("Corpo da requisição inválido (JSON malformado).");
            return await ResponderErroAsync(request, HttpStatusCode.BadRequest,
                "Corpo da requisição inválido.", correlationId);
        }

        if (CpfValueObject.TryCriar(corpo?.Cpf, out var cpf, out var erroCpf) is false)
        {
            // Não logamos o CPF recebido: é dado pessoal e a entrada pode nem ser um CPF válido.
            _logger.LogWarning("Tentativa de autenticação com CPF inválido: {Erro}", erroCpf);
            return await ResponderErroAsync(request, HttpStatusCode.BadRequest,
                erroCpf ?? "CPF inválido.", correlationId);
        }

        var cliente = await _clienteRepository.ObterPorCpfAsync(cpf!.Valor, cancellationToken);

        if (cliente is null)
        {
            _logger.LogWarning("Cliente não encontrado para o CPF informado.");
            return await ResponderErroAsync(request, HttpStatusCode.NotFound,
                "Cliente não encontrado.", correlationId);
        }

        if (cliente.Ativo is false)
        {
            _logger.LogWarning("Cliente {ClienteId} está inativo.", cliente.Id);
            return await ResponderErroAsync(request, HttpStatusCode.Forbidden,
                "Cliente inativo.", correlationId);
        }

        var token = _tokenService.GerarToken(cliente);

        _logger.LogInformation("Token emitido para o cliente {ClienteId}.", cliente.Id);

        return await ResponderAsync(request, HttpStatusCode.OK, token, correlationId);
    }

    /// <summary>Health check da própria Function — usado pelo synthetics do Datadog.</summary>
    [Function("Health")]
    public static HttpResponseData Health(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData request)
    {
        var response = request.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        response.WriteString("""{"status":"healthy"}""");
        return response;
    }

    private static string ObterCorrelationId(HttpRequestData request)
    {
        if (request.Headers.TryGetValues(CorrelationIdHeader, out var valores))
        {
            var valor = valores.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(valor) is false)
                return valor;
        }

        return Guid.NewGuid().ToString();
    }

    private static Task<HttpResponseData> ResponderErroAsync(
        HttpRequestData request, HttpStatusCode status, string mensagem, string correlationId)
        => ResponderAsync(request, status, new ErroResponse(mensagem, correlationId), correlationId);

    private static async Task<HttpResponseData> ResponderAsync<T>(
        HttpRequestData request, HttpStatusCode status, T corpo, string correlationId)
    {
        var response = request.CreateResponse(status);
        response.Headers.Add(CorrelationIdHeader, correlationId);
        await response.WriteAsJsonAsync(corpo, status);
        return response;
    }
}
