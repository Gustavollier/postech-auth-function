namespace PosTech.AuthFunction.Models;

/// <summary>
/// Projeção mínima da tabela Cliente necessária para autenticar.
/// Espelha PosTechChallenge.Dominio.Model.Cliente (apenas os campos usados aqui).
/// </summary>
public sealed class Cliente
{
    public int Id { get; set; }

    public string? CPF { get; set; }

    public string? NomeCompleto { get; set; }

    public string? Email { get; set; }

    /// <summary>
    /// Status do cliente. Um cliente com Ativo = false existe na base mas está desativado,
    /// e por isso recebe 403 em vez de 404.
    /// </summary>
    public bool Ativo { get; set; }
}
