using Dapper;
using InvestLens.Application.DTOs.Plano;
using InvestLens.Application.Interfaces.Repositories;
using InvestLens.Infrastructure.Database.Connection;

namespace InvestLens.Infrastructure.Repositories
{
    public sealed class PlanoRepository(IDbConnectionFactory factory) : IPlanoRepository
    {
        public async Task<IReadOnlyList<PlanoResponse>> ListarAtivosAsync(CancellationToken ct)
        {
            await using var connection = factory.CreateConnection();

            var sql = @"SELECT ID_Plano IdPlano,
                               NM_Plano Nome,
                               VL_Plano Valor
                        FROM dbo.ILCAD002
                        WHERE FL_Ativo = 1
                        ORDER BY VL_Plano, NM_Plano";

            var retorno = await connection.QueryAsync<PlanoResponse>(
                new CommandDefinition(sql, cancellationToken: ct));

            return retorno.AsList();
        }

        public async Task<PlanoResponse?> ObterAtivoAsync(int id, CancellationToken ct)
        {
            await using var connection = factory.CreateConnection();

            var sql = @"SELECT ID_Plano IdPlano,
                               NM_Plano Nome,
                               VL_Plano Valor
                        FROM dbo.ILCAD002
                        WHERE FL_Ativo = 1
                          AND ID_Plano = @IdPlano";

            return await connection.QueryFirstOrDefaultAsync<PlanoResponse>(
                new CommandDefinition(sql, new { IdPlano = id }, cancellationToken: ct));
        }
    }
}
