using System.Data;
using Dapper;
using InvestLens.Application.DTOs.Assinatura;
using InvestLens.Application.Interfaces.Repositories;
using InvestLens.Infrastructure.Database.Connection;
using InvestLens.Infrastructure.Database.Extensions;
using Microsoft.Data.SqlClient;

namespace InvestLens.Infrastructure.Repositories;

public sealed class SolicitacaoRepository(IDbConnectionFactory factory) : ISolicitacaoRepository
{
    private const string Consulta = """
        SELECT S.ID_Solicitacao AS IdSolicitacao, S.CD_Tipo AS Tipo, S.CD_Status AS Status,
               Origem.NM_Plano AS PlanoOrigem, Destino.NM_Plano AS PlanoDestino,
               S.DH_Inclusao AS DataSolicitacao
        FROM dbo.ILCAD005 S
        INNER JOIN dbo.ILCAD002 Origem ON Origem.ID_Plano = S.ID_Plano_Origem
        LEFT JOIN dbo.ILCAD002 Destino ON Destino.ID_Plano = S.ID_Plano_Destino
        WHERE S.ID_Usuario = @IdUsuario
        """;

    public async Task<IReadOnlyList<SolicitacaoResponse>> ListarAsync(int idUsuario, CancellationToken cancellationToken)
    {
        await using var connection = factory.CreateConnection();
        var rows = await connection.QueryAsync<SolicitacaoResponse>(new CommandDefinition(Consulta + " ORDER BY S.ID_Solicitacao DESC", new { IdUsuario = idUsuario }, cancellationToken: cancellationToken));
        return rows.AsList();
    }

    public async Task<SolicitacaoResponse> RegistrarAsync(int idUsuario, string tipo, int? destino, CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@ID_Usuario", idUsuario, DbType.Int32);
        parameters.Add("@CD_Tipo", tipo, DbType.AnsiStringFixedLength, size: 3);
        parameters.Add("@ID_Plano_Destino", destino, DbType.Int32);
        parameters.Add("@ID_Solicitacao", dbType: DbType.Int32, direction: ParameterDirection.Output);
        parameters.AdicionarReturnCodeErrMsg();
        await using var connection = factory.CreateConnection();
        try
        {
            await connection.ExecuteAsync(new CommandDefinition("dbo.st_RegistrarSolicitacao", parameters, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));
        }
        catch (SqlException ex)
        {
            parameters.ValidarReturnCodeErrMsg(ex);
            throw;
        }
        parameters.ValidarReturnCodeErrMsg();
        return await connection.QuerySingleAsync<SolicitacaoResponse>(new CommandDefinition(Consulta + " AND S.ID_Solicitacao = @IdSolicitacao", new { IdUsuario = idUsuario, IdSolicitacao = parameters.Get<int>("@ID_Solicitacao") }, cancellationToken: cancellationToken));
    }
}
