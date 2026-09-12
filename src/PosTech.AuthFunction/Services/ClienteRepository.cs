using Dapper;
using Microsoft.Data.SqlClient;
using PosTech.AuthFunction.Models;

namespace PosTech.AuthFunction.Services;

public interface IClienteRepository
{
    Task<Cliente?> ObterPorCpfAsync(string cpf, CancellationToken cancellationToken = default);
}

public sealed class ClienteRepository : IClienteRepository
{
    /// <summary>
    /// Diferente de ClienteQuerys.OBTER_POR_CPF_CNPJ na API, esta query NÃO filtra por Ativo = 1.
    /// A Function precisa distinguir "CPF não cadastrado" (404) de "cliente inativo" (403),
    /// e o filtro colapsaria os dois casos em "não encontrado".
    /// </summary>
    private const string ObterPorCpf = @"
        SELECT TOP 1 Id, CPF, NomeCompleto, Email, Ativo
        FROM Cliente
        WHERE CPF = @Cpf";

    private readonly string _connectionString;

    public ClienteRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public async Task<Cliente?> ObterPorCpfAsync(string cpf, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);

        var command = new CommandDefinition(
            ObterPorCpf,
            new { Cpf = cpf },
            cancellationToken: cancellationToken);

        return await connection.QueryFirstOrDefaultAsync<Cliente>(command);
    }
}
