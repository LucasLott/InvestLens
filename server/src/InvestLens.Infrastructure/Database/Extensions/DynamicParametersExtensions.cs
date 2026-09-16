using System.Data;
using Dapper;
using InvestLens.Domain.Exceptions;

namespace InvestLens.Infrastructure.Database.Extensions
{
    public static class DynamicParametersExtensions
    {
        public static void AdicionarReturnCodeErrMsg(this DynamicParameters parameters)
        {
            parameters.Add("@ReturnCode", dbType: DbType.Int16, direction: ParameterDirection.Output);
            parameters.Add("@ErrMsg", dbType: DbType.AnsiString, size: 255, direction: ParameterDirection.Output);
        }
        
        public static void ValidarReturnCodeErrMsg(this DynamicParameters parameters)
        {
            short? code;
            string? message;
            try
            {
                code = parameters.Get<short?>("@ReturnCode");
                message = parameters.Get<string?>("@ErrMsg");
            }
            catch (Exception exception) when (exception is KeyNotFoundException or InvalidCastException)
            {
                throw new DatabaseException("Parâmetros de saída ausentes ou inválidos.", exception);
            }
            // ErrMsg pode conter SQL ou secrets; nunca publicar ou registrar seu conteúdo.
            switch (code)
            {
                case 0 when message is null: return;
                case 1: throw new BusinessException();
                case 2: throw new DatabaseException("A procedure informou uma falha técnica.");
                default: throw new DatabaseException("Contrato de saída da procedure inválido.");
            }
        }
    }
}