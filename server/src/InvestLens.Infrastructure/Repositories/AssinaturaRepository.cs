using Dapper;
using InvestLens.Application.Interfaces.Repositories;
using InvestLens.Application.Models;
using InvestLens.Infrastructure.Database.Connection;

namespace InvestLens.Infrastructure.Repositories
{
    public sealed class AssinaturaRepository(IDbConnectionFactory factory) : IAssinaturaRepository
    {
        public async Task<bool> PossuiPagamentoConfirmadoAsync(int idUsuario, CancellationToken cancellationToken)
        {
            await using var connection = factory.CreateConnection();
            return await connection.ExecuteScalarAsync<bool>(new CommandDefinition("""
                SELECT CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.ILCAD003 A
                  INNER JOIN dbo.ILCAD004 P ON P.ID_Assinatura = A.ID_Assinatura AND P.ID_Usuario = A.ID_Usuario AND P.ID_Plano = A.ID_Plano
                  WHERE A.ID_Usuario = @IdUsuario AND P.CD_Status = 'CONFIRMADO' AND P.DH_Pagamento >= A.DH_Inicio_Assinatura AND P.DH_Pagamento < A.DH_Fim_Assinatura AND P.DH_Pagamento <= SYSUTCDATETIME())
                  THEN 1 ELSE 0 END AS BIT)
                """, new { IdUsuario = idUsuario }, cancellationToken: cancellationToken));
        }

        public async Task<AssinaturaAtual?> ObterAtualAsync(int idUsuario, CancellationToken cancellationToken)
        {
            const string sql = @"SELECT Cadastro.NM_Plano AS PlanoCadastrado
                                       ,AssinaturaPlano.NM_Plano AS PlanoAssinatura
                                       ,Assinatura.CD_Status AS StatusAssinatura
                                       ,Assinatura.DH_Inicio_Assinatura AS InicioVigencia
                                       ,Assinatura.DH_Fim_Assinatura AS FimVigencia
                                 FROM dbo.ILCAD001 Usuario
                                      INNER JOIN dbo.ILCAD002 Cadastro
                                      ON Cadastro.ID_Plano = Usuario.ID_Plano
                                      LEFT JOIN dbo.ILCAD003 Assinatura
                                      ON Assinatura.ID_Usuario = Usuario.ID_Usuario
                                     LEFT JOIN dbo.ILCAD002 AssinaturaPlano
                                     ON AssinaturaPlano.ID_Plano = Assinatura.ID_Plano
                                 WHERE Usuario.ID_Usuario = @IdUsuario";

            await using var connection = factory.CreateConnection();

            return await connection.QueryFirstOrDefaultAsync<AssinaturaAtual>(new CommandDefinition(sql,
                                                                                                    new { IdUsuario = idUsuario },
                                                                                                    cancellationToken: cancellationToken));
        }
    }
}
