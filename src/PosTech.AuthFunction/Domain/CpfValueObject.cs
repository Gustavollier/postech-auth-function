namespace PosTech.AuthFunction.Domain;

/// <summary>
/// Validação de CPF. Portado de PosTechChallenge.Dominio.ValueObjects.CpfValueObject
/// para manter a mesma regra de validação entre a API e esta Function.
/// </summary>
public class CpfValueObject
{
    private const int TamanhoCpf = 11;

    public string Valor { get; }

    public CpfValueObject(string cpf)
    {
        var cpfLimpo = ExtrairDigitos(cpf);

        if (string.IsNullOrWhiteSpace(cpf))
            throw new ArgumentException("CPF não pode ser vazio.");

        if (cpfLimpo.Length != TamanhoCpf)
            throw new ArgumentException("CPF deve conter 11 dígitos.");

        if (cpfLimpo.Distinct().Count() == 1)
            throw new ArgumentException("CPF inválido.");

        if (!ValidarDigitosVerificadores(cpfLimpo))
            throw new ArgumentException("CPF inválido.");

        Valor = cpfLimpo;
    }

    /// <summary>
    /// Variante sem exceção, usada no fluxo da Function para devolver 400 com a mensagem de erro.
    /// </summary>
    public static bool TryCriar(string? cpf, out CpfValueObject? resultado, out string? erro)
    {
        try
        {
            resultado = new CpfValueObject(cpf ?? string.Empty);
            erro = null;
            return true;
        }
        catch (ArgumentException ex)
        {
            resultado = null;
            erro = ex.Message;
            return false;
        }
    }

    public static implicit operator string(CpfValueObject cpf) => cpf.Valor;

    public override string ToString() => Valor;

    public override bool Equals(object? obj) => obj is CpfValueObject cpf && cpf.Valor == Valor;

    public override int GetHashCode() => Valor.GetHashCode();

    private static bool ValidarDigitosVerificadores(string cpf)
    {
        var primeiroDigito = CalcularDigito(cpf, 10, 9);
        var segundoDigito = CalcularDigito(cpf, 11, 10);

        return cpf[9] - '0' == primeiroDigito && cpf[10] - '0' == segundoDigito;
    }

    private static int CalcularDigito(string cpf, int pesoInicial, int quantidadeDigitos)
    {
        var soma = 0;

        for (var indice = 0; indice < quantidadeDigitos; indice++)
        {
            soma += (cpf[indice] - '0') * (pesoInicial - indice);
        }

        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }

    private static string ExtrairDigitos(string valor)
    {
        return new string((valor ?? string.Empty).Where(char.IsDigit).ToArray());
    }
}
