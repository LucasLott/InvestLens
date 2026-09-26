using System.Data;
using Dapper;
using InvestLens.Application.Interfaces.Repositories;
using InvestLens.Infrastructure.Database.Connection;

namespace InvestLens.Infrastructure.Repositories;

public sealed class RefreshTokenRepository(IDbConnectionFactory factory) : IRefreshTokenRepository
{
    public async Task CreateAsync(int userId, string hash, DateTime expiresAt, CancellationToken cancellationToken)
    {
        await using var connection = factory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition("dbo.st_RefreshToken",
            new { DS_Operacao = "criar", ID_Usuario = userId, TX_Hash = hash, DH_Expiracao = expiresAt },
            commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));
    }

    public async Task<RefreshSession?> RotateAsync(string hash, string replacementHash, CancellationToken cancellationToken)
    {
        await using var connection = factory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<RefreshSession>(new CommandDefinition("dbo.st_RefreshToken",
            new { DS_Operacao = "rotacionar", TX_Hash = hash, TX_HashSubstituto = replacementHash },
            commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));
    }

    public async Task RevokeAsync(string hash, CancellationToken cancellationToken)
    {
        await using var connection = factory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition("dbo.st_RefreshToken",
            new { DS_Operacao = "revogar", TX_Hash = hash },
            commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));
    }
}
