using Dapper;
using InvestLens.Application.Interfaces.Repositories;
using InvestLens.Application.Models;
using InvestLens.Infrastructure.Database.Connection;

namespace InvestLens.Infrastructure.Repositories
{
    public sealed class AssinaturaRepository(IDbConnectionFactory factory) : IAssinaturaRepository
    {
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