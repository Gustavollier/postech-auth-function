using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using PosTech.AuthFunction.Models;

namespace PosTech.AuthFunction.Services;

public interface ITokenService
{
    AuthResponse GerarToken(Cliente cliente);
}

/// <summary>
/// Emite um JWT aceito pela API principal sem nenhuma alteração nela.
/// A API valida com ValidateIssuerSigningKey/ValidateIssuer/ValidateAudience/ValidateLifetime
/// e ClockSkew = 0 (ver PosTechChallenge/Program.cs), então issuer, audience, algoritmo e
/// segredo precisam bater exatamente com os da aplicação.
/// </summary>
public sealed class TokenService : ITokenService
{
    public const int MinimoBytesSegredo = 32;

    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expirationMinutes;

    public TokenService(string secretKey, string issuer, string audience, int expirationMinutes)
    {
        if (string.IsNullOrWhiteSpace(secretKey))
            throw new ArgumentException("Jwt:SecretKey não configurado.", nameof(secretKey));

        // Mesma checagem feita pela API na inicialização — falhar aqui é melhor do que
        // emitir um token que a API vai rejeitar.
        if (Encoding.UTF8.GetByteCount(secretKey) < MinimoBytesSegredo)
            throw new ArgumentException($"Jwt:SecretKey deve ter ao menos {MinimoBytesSegredo} bytes.", nameof(secretKey));

        _secretKey = secretKey;
        _issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
        _audience = audience ?? throw new ArgumentNullException(nameof(audience));
        _expirationMinutes = expirationMinutes > 0 ? expirationMinutes : 15;
    }

    public AuthResponse GerarToken(Cliente cliente)
    {
        ArgumentNullException.ThrowIfNull(cliente);

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, cliente.Id.ToString()),
            new(ClaimTypes.Role, "Cliente"),
            new("ClienteId", cliente.Id.ToString()),
            new("cpf", cliente.CPF ?? string.Empty)
        };

        if (string.IsNullOrWhiteSpace(cliente.NomeCompleto) is false)
            claims.Add(new Claim(ClaimTypes.Name, cliente.NomeCompleto));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_expirationMinutes),
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return new AuthResponse(
            AccessToken: tokenHandler.WriteToken(token),
            TokenType: "Bearer",
            ExpiresIn: _expirationMinutes * 60,
            ClienteId: cliente.Id,
            Nome: cliente.NomeCompleto);
    }
}
