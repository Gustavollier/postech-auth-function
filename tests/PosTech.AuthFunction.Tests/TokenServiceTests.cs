using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using PosTech.AuthFunction.Models;
using PosTech.AuthFunction.Services;
using Xunit;

namespace PosTech.AuthFunction.Tests;

public class TokenServiceTests
{
    private const string Segredo = "MinhaSenhaProtegida123MinhaSenhaProtegida123";
    private const string Issuer = "PosTechChallenge";
    private const string Audience = "PosTechChallenge-API";

    private static TokenService CriarServico(int expiracaoMinutos = 15)
        => new(Segredo, Issuer, Audience, expiracaoMinutos);

    private static Cliente ClienteValido() => new()
    {
        Id = 42,
        CPF = "12345678901",
        NomeCompleto = "Joao da Silva",
        Email = "joao@exemplo.com",
        Ativo = true
    };

    /// <summary>
    /// Réplica exata dos TokenValidationParameters de PosTechChallenge/Program.cs.
    /// Se este teste passar, a API principal aceita o token desta Function sem alteração.
    /// </summary>
    private static TokenValidationParameters ParametrosDaApi() => new()
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Segredo)),
        ValidateIssuer = true,
        ValidIssuer = Issuer,
        ValidateAudience = true,
        ValidAudience = Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    [Fact]
    public void GerarToken_ProduzTokenAceitoPelosParametrosDaApi()
    {
        var token = CriarServico().GerarToken(ClienteValido());

        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(token.AccessToken, ParametrosDaApi(), out var validado);

        Assert.NotNull(principal);
        Assert.IsType<JwtSecurityToken>(validado);
        Assert.Equal(SecurityAlgorithms.HmacSha256, ((JwtSecurityToken)validado).Header.Alg);
    }

    [Fact]
    public void GerarToken_IncluiClaimsDoCliente()
    {
        var cliente = ClienteValido();

        var token = CriarServico().GerarToken(cliente);

        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(token.AccessToken, ParametrosDaApi(), out _);

        Assert.Equal(cliente.Id.ToString(), principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal("Cliente", principal.FindFirst(ClaimTypes.Role)?.Value);
        Assert.Equal(cliente.Id.ToString(), principal.FindFirst("ClienteId")?.Value);
        Assert.Equal(cliente.CPF, principal.FindFirst("cpf")?.Value);
        Assert.Equal(cliente.NomeCompleto, principal.FindFirst(ClaimTypes.Name)?.Value);
    }

    [Fact]
    public void GerarToken_RetornaMetadadosCoerentes()
    {
        var cliente = ClienteValido();

        var token = CriarServico(expiracaoMinutos: 30).GerarToken(cliente);

        Assert.Equal("Bearer", token.TokenType);
        Assert.Equal(30 * 60, token.ExpiresIn);
        Assert.Equal(cliente.Id, token.ClienteId);
        Assert.Equal(cliente.NomeCompleto, token.Nome);
        Assert.False(string.IsNullOrWhiteSpace(token.AccessToken));
    }

    [Fact]
    public void Construtor_ComSegredoCurto_LancaArgumentException()
    {
        // A API exige >= 32 bytes e falha na inicialização se for menor.
        var curto = new string('a', TokenService.MinimoBytesSegredo - 1);

        var excecao = Assert.Throws<ArgumentException>(
            () => new TokenService(curto, Issuer, Audience, 15));

        Assert.Contains("32 bytes", excecao.Message);
    }

    [Fact]
    public void Construtor_ComSegredoVazio_LancaArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new TokenService("", Issuer, Audience, 15));
    }

    [Fact]
    public void GerarToken_ComAudienceDiferente_NaoEhAceitoPelaApi()
    {
        // Guarda-chuva contra configurar a Function com audience errada e só descobrir em produção.
        var servico = new TokenService(Segredo, Issuer, "outra-audience", 15);

        var token = servico.GerarToken(ClienteValido());

        var handler = new JwtSecurityTokenHandler();
        Assert.Throws<SecurityTokenInvalidAudienceException>(
            () => handler.ValidateToken(token.AccessToken, ParametrosDaApi(), out _));
    }
}
