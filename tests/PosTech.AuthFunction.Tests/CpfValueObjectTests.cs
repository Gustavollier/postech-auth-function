using PosTech.AuthFunction.Domain;
using Xunit;

namespace PosTech.AuthFunction.Tests;

public class CpfValueObjectTests
{
    [Theory]
    [InlineData("12345678909")]
    [InlineData("123.456.789-09")]
    [InlineData("529.982.247-25")]
    public void Construtor_ComCpfValido_NormalizaParaDigitos(string cpf)
    {
        var resultado = new CpfValueObject(cpf);

        Assert.Equal(11, resultado.Valor.Length);
        Assert.All(resultado.Valor, caractere => Assert.True(char.IsDigit(caractere)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123")]
    [InlineData("123456789012")]
    [InlineData("11111111111")]
    [InlineData("12345678900")]
    public void Construtor_ComCpfInvalido_LancaArgumentException(string cpf)
    {
        Assert.Throws<ArgumentException>(() => new CpfValueObject(cpf));
    }

    [Fact]
    public void TryCriar_ComCpfValido_RetornaTrueSemErro()
    {
        var sucesso = CpfValueObject.TryCriar("529.982.247-25", out var cpf, out var erro);

        Assert.True(sucesso);
        Assert.NotNull(cpf);
        Assert.Null(erro);
        Assert.Equal("52998224725", cpf!.Valor);
    }

    [Fact]
    public void TryCriar_ComCpfInvalido_RetornaFalseComMensagem()
    {
        var sucesso = CpfValueObject.TryCriar("11111111111", out var cpf, out var erro);

        Assert.False(sucesso);
        Assert.Null(cpf);
        Assert.Equal("CPF inválido.", erro);
    }

    [Fact]
    public void TryCriar_ComNulo_RetornaFalse()
    {
        var sucesso = CpfValueObject.TryCriar(null, out _, out var erro);

        Assert.False(sucesso);
        Assert.NotNull(erro);
    }
}
