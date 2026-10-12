using Dapper;
using InvestLens.Application.Interfaces.Repositories;
using InvestLens.Infrastructure.Database.Connection;

namespace InvestLens.Infrastructure.Repositories;

public sealed class FinancialUpdateRepository(IDbConnectionFactory factory) : IFinancialUpdateRepository
{
    public async Task<IReadOnlyList<string>> GetTickersAsync(int limit, CancellationToken cancellationToken)
    {
        const string sql = "SELECT TOP (@Limit) CD_Ticker FROM dbo.ILFIN002 ORDER BY DH_Alteracao ASC, DH_Inclusao ASC, CD_Ticker ASC";
        await using var connection = factory.CreateConnection();
        var values = await connection.QueryAsync<string>(new CommandDefinition(sql, new { Limit = limit }, cancellationToken: cancellationToken));
        return values.AsList();
    }
}
