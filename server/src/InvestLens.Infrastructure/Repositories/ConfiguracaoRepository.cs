using System.Data;
using Dapper;
using InvestLens.Application.DTOs.Configuracao;
using InvestLens.Application.Interfaces.Repositories.Configuracao;
using InvestLens.Infrastructure.Database.Connection;
using InvestLens.Infrastructure.Database.Extensions;
using Microsoft.Data.SqlClient;

namespace InvestLens.Infrastructure.Repositories
{
    public sealed class ConfiguracaoRepository(IDbConnectionFactory connectionFactory) : IConfiguracaoRepository
    {
        public async Task<ConfiguracaoResponse?> ObterAsync(int idUsuario, CancellationToken cancellationToken)
        {
            const string sql = @"SELECT ID_Configuracao AS IdConfiguracao
                                        ,ID_Usuario AS IdUsuario
                                        ,PE_PL_Maximo AS PLMaximo
                                        ,PE_DY_Minimo AS DYMinimo
                                        ,PE_ROE_Minimo AS ROEMinimo
                                        ,PE_DividaPatrimonio_Maximo AS DividaPatrimonioMaximo
                                        ,PE_MargemLiquida_Minimo AS MargemLiquidaMinimo
                                        ,PE_RetornoPreco AS RetornoPreco
                                        ,DH_Inclusao AS DataInclusao
                                        ,DH_Alteracao AS DataAlteracao
                                  FROM dbo.fn_DadosConfiguracao(@ID_Usuario)";

            var parameters = new DynamicParameters();
            parameters.Add("@ID_Usuario", idUsuario, DbType.Int32);

            await using var connection = connectionFactory.CreateConnection();
            var command = new CommandDefinition(commandText: sql,
                                                parameters: parameters,
                                                commandType: CommandType.Text,
                                                cancellationToken: cancellationToken);
            return await connection.QueryFirstOrDefaultAsync<ConfiguracaoResponse>(command);
        }

        public Task AdicionarAsync(int idUsuario, ConfiguracaoRequest request, CancellationToken cancellationToken) =>
            ExecutarAsync("dbo.st_ConfiguracaoAdd", idUsuario, request, cancellationToken);

        public Task AlterarAsync(int idUsuario, ConfiguracaoRequest request, CancellationToken cancellationToken) =>
            ExecutarAsync("dbo.st_ConfiguracaoUpd", idUsuario, request, cancellationToken);

        private async Task ExecutarAsync(string procedure, int idUsuario, ConfiguracaoRequest request, CancellationToken cancellationToken)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@ID_Usuario", idUsuario, DbType.Int32);
            parameters.Add("@PE_PL_Maximo", request.PLMaximo!.Value, DbType.Decimal, precision: 8, scale: 4);
            parameters.Add("@PE_DY_Minimo", request.DYMinimo!.Value, DbType.Decimal, precision: 8, scale: 4);
            parameters.Add("@PE_ROE_Minimo", request.ROEMinimo!.Value, DbType.Decimal, precision: 8, scale: 4);
            parameters.Add("@PE_DividaPatrimonio_Maximo", request.DividaPatrimonioMaximo!.Value, DbType.Decimal, precision: 8, scale: 4);
            parameters.Add("@PE_MargemLiquida_Minimo", request.MargemLiquidaMinimo!.Value, DbType.Decimal, precision: 8, scale: 4);
            parameters.Add("@PE_RetornoPreco", request.RetornoPreco!.Value, DbType.Decimal, precision: 8, scale: 4);
            parameters.AdicionarReturnCodeErrMsg();

            await using var connection = connectionFactory.CreateConnection();
            var command = new CommandDefinition(commandText: procedure,
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
