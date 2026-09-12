using System.Text.Json.Serialization;

namespace PosTech.AuthFunction.Models;

/// <summary>Corpo esperado em POST /auth.</summary>
public sealed class AuthRequest
{
    [JsonPropertyName("cpf")]
    public string? Cpf { get; set; }
}

/// <summary>Resposta 200 de POST /auth.</summary>
public sealed record AuthResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("token_type")] string TokenType,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("cliente_id")] int ClienteId,
    [property: JsonPropertyName("nome")] string? Nome);

/// <summary>Resposta de erro padronizada (400/403/404/500).</summary>
public sealed record ErroResponse(
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("correlationId")] string? CorrelationId = null);
