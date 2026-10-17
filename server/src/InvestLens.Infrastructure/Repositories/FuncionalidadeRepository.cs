using Dapper;
using InvestLens.Application.Interfaces.Repositories;
using InvestLens.Infrastructure.Database.Connection;

namespace InvestLens.Infrastructure.Repositories;

public sealed class FuncionalidadeRepository(IDbConnectionFactory factory) : IFuncionalidadeRepository
{
    public async Task<IReadOnlyList<(int Id, string Nome, string Pro)>> ListarAsync(CancellationToken cancellationToken)
    {
        await using var connection = factory.CreateConnection();
        var itens = await connection.QueryAsync<(int Id, string Nome, string Pro)>(new CommandDefinition(
            "SELECT ID_Funcionalidade, NM_Funcionalidade, SN_Pro FROM dbo.ILCAD006 ORDER BY NM_Funcionalidade", cancellationToken: cancellationToken));
        return itens.AsList();
    }

    public async Task<bool> PossuiPermissaoAsync(string planoEfetivo, string chave, CancellationToken cancellationToken)
    {
        await using var connection = factory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition("""
            SELECT CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.ILCAD006
              WHERE NM_Funcionalidade = @Chave AND (SN_Pro COLLATE Latin1_General_100_BIN2 = 'N' OR (SN_Pro COLLATE Latin1_General_100_BIN2 = 'S' AND @Plano = 'Pro')))
              THEN 1 ELSE 0 END AS BIT)
            """, new { Chave = chave, Plano = planoEfetivo }, cancellationToken: cancellationToken));
    }

}
