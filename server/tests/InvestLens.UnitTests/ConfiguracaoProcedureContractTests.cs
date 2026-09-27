using System.Reflection;
using Dapper;
using InvestLens.Application.Exceptions;
using InvestLens.Infrastructure.Database.Extensions;
using Microsoft.Data.SqlClient;

namespace InvestLens.UnitTests
{
    public class ConfiguracaoProcedureContractTests
    {
        [Theory]
        [InlineData("st_ConfiguracaoAdd", 2, typeof(ConfiguracaoNaoEncontradaException))]
        [InlineData("st_ConfiguracaoAdd", 3, typeof(ConfiguracaoJaExisteException))]
        [InlineData("st_ConfiguracaoUpd", 2, typeof(ConfiguracaoNaoEncontradaException))]
        [InlineData("st_ConfiguracaoUpd", 3, typeof(ConfiguracaoNaoEncontradaException))]
        public void EstadosConhecidosDasProceduresMapeiamSemInspecionarMensagem(string procedure, byte state, Type exceptionType)
        {
            var parameters = new DynamicParameters();
            var exception = Assert.Throws(exceptionType, () =>
                parameters.ValidarReturnCodeErrMsg(CriarSqlException(50001, state, procedure)));

            Assert.DoesNotContain("sensitive-marker", exception.Message);
        }

        [Theory]
        [InlineData(2601)]
        [InlineData(2627)]
        public void ViolacaoDeUnicidadeConcorrenteRetornaConflito(int number)
        {
            var parameters = new DynamicParameters();
            Assert.IsType<ConfiguracaoJaExisteException>(Assert.ThrowsAny<Exception>(() =>
                parameters.ValidarReturnCodeErrMsg(CriarSqlException(number, 1, "st_ConfiguracaoAdd"))));
        }

        [Fact]
        public void EstadoGenericoDaProcedurePermaneceErroDeNegocio()
        {
            var parameters = new DynamicParameters();
            Assert.IsType<InvestLens.Domain.Exceptions.BusinessException>(Assert.ThrowsAny<Exception>(() =>
                parameters.ValidarReturnCodeErrMsg(CriarSqlException(50001, 1, "st_ConfiguracaoAdd"))));
        }

        private static SqlException CriarSqlException(int number, byte state, string procedure)
        {
            var constructor = typeof(SqlError).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
                .Single(x => x.GetParameters().Length == 9 && x.GetParameters()[8].ParameterType == typeof(Exception));
            var error = (SqlError)constructor.Invoke(new object?[]
            {
                number, state, (byte)16, "test-server", "sensitive-marker", procedure, 1, 0, null
            });
            var errors = (SqlErrorCollection)Activator.CreateInstance(typeof(SqlErrorCollection), nonPublic: true)!;
            typeof(SqlErrorCollection).GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(errors, [error]);
            var factory = typeof(SqlException).GetMethod("CreateException", BindingFlags.Static | BindingFlags.NonPublic,
                binder: null, types: [typeof(SqlErrorCollection), typeof(string)], modifiers: null)!;
            return (SqlException)factory.Invoke(null, [errors, "16.0"])!;
        }
    }
}
