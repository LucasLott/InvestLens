using System.Data;
using Dapper;
using InvestLens.Application.DTOs.Usuario;
using InvestLens.Application.Interfaces.Repositories.Usuario;
using InvestLens.Application.Models;
using InvestLens.Infrastructure.Database.Connection;
using InvestLens.Infrastructure.Database.Extensions;
using Microsoft.Data.SqlClient;

namespace InvestLens.Infrastructure.Repositories
{
    public sealed class UsuarioRepository(IDbConnectionFactory connectionFactory) : IUsuarioRepository
    {
        private readonly IDbConnectionFactory _connectionFactory = connectionFactory;

        public async Task<DadosLogin?> ObterDadosLoginAsync(string email, CancellationToken cancellationToken)
        {
            var sql = @"SELECT ID_Usuario AS IdUsuario
                              ,CD_Usuario AS Codigo
                              ,NM_Usuario AS Nome
                              ,DS_Email AS Email
                              ,TX_Senha AS SenhaHash
                              ,FL_Ativo AS Ativo
                        FROM dbo.fn_DadosLogin(@DS_Email)";

            var parameters = new DynamicParameters();

            parameters.Add("@DS_Email", email, DbType.AnsiString, size: 255);

            await using var connection = _connectionFactory.CreateConnection();
            var command = new CommandDefinition(commandText: sql,
                                                parameters: parameters,
                                                commandType: CommandType.Text,
                                                cancellationToken: cancellationToken);

            return await connection.QueryFirstOrDefaultAsync<DadosLogin>(command);
        }

        public async Task Adicionar(AdicionarUsuarioRequest request, string senhaHash, CancellationToken cancellationToken)
        {
            var parameters = new DynamicParameters();

            parameters.Add("@NM_Usuario", request.Nome, DbType.AnsiString, size: 80);
            parameters.Add("@DS_Email", request.Email, DbType.AnsiString, size: 255);
            parameters.Add("@TX_Senha", senhaHash, DbType.AnsiString, size: -1);
            parameters.Add("@FL_Ativo", true);

            parameters.AdicionarReturnCodeErrMsg();

            await using var connection = _connectionFactory.CreateConnection();

            var command = new CommandDefinition(commandText: "dbo.st_UsuarioAdd",
                                                parameters: parameters,
                                                commandType: CommandType.StoredProcedure,
                                                cancellationToken: cancellationToken);

            try
            {
                await connection.ExecuteAsync(command);
            }
            catch (SqlException exception)
            {
                parameters.ValidarReturnCodeErrMsg(exception);
                throw;
            }

            parameters.ValidarReturnCodeErrMsg();
        }
    }
}
