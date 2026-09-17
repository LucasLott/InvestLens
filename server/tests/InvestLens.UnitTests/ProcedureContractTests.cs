using Dapper;
using InvestLens.Domain.Exceptions;
using InvestLens.Infrastructure.Database;
using InvestLens.Infrastructure.Database.Extensions;
using Microsoft.Data.SqlClient;
using System.Reflection;

namespace InvestLens.UnitTests
{
    public class ProcedureContractTests
    {
        [Theory]
        [InlineData("st_UsuarioAdd")]
        [InlineData("dbo.st_UsuarioAdd")]
        public void ControlledSqlErrorIsBusinessFailureEvenWithoutOutputParameters(string procedure)
        {
            var parameters = new DynamicParameters();
            var sqlException = CreateSqlException(50001, 1, procedure);
            var exception = Assert.Throws<BusinessException>(() => parameters.ValidarReturnCodeErrMsg(sqlException));
            Assert.DoesNotContain("sensitive-marker", exception.Message);
        }

        [Theory]
        [InlineData(50001, 2, "st_UsuarioAdd")]
        [InlineData(50001, 1, "AnotherProcedure")]
        [InlineData(2627, 1, "st_UsuarioAdd")]
        [InlineData(-2, 0, "")]
        public void UnknownSqlFailureCannotBeMaskedByOutputParameters(int number, byte state, string procedure)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@ReturnCode", (short)0);
            parameters.Add("@ErrMsg", null);
            var sqlException = CreateSqlException(number, state, procedure);
            var exception = Assert.Throws<DatabaseException>(() => parameters.ValidarReturnCodeErrMsg(sqlException));
            Assert.Same(sqlException, exception.InnerException);
            Assert.DoesNotContain("sensitive-marker", exception.Message);
        }

        // SqlClient não expõe construtores públicos; a reflexão fica restrita ao double de teste.
        private static SqlException CreateSqlException(int number, byte state, string procedure)
        {
            var constructor = typeof(SqlError).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
                .Single(x => x.GetParameters().Length == 9 && x.GetParameters()[8].ParameterType == typeof(Exception));
            var error = (SqlError)constructor.Invoke(new object?[]
            {
                number, state, (byte)16, "test-server", "sensitive-marker", procedure, 1, 0, null
            });
            var errors = (SqlErrorCollection)Activator.CreateInstance(typeof(SqlErrorCollection), nonPublic: true)!;
            typeof(SqlErrorCollection).GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(errors, new object[] { error });
            var factory = typeof(SqlException).GetMethod("CreateException", BindingFlags.Static | BindingFlags.NonPublic,
                binder: null, types: new[] { typeof(SqlErrorCollection), typeof(string) }, modifiers: null)!;
            return (SqlException)factory.Invoke(null, new object[] { errors, "16.0" })!;
        }

        [Fact]
        public void SuccessRequiresBothOutputParametersAndNullMessage()
        {
            var parameters = new DynamicParameters();
            Assert.Throws<DatabaseException>(() => parameters.ValidarReturnCodeErrMsg());
            parameters.AdicionarReturnCodeErrMsg();
            Assert.Throws<DatabaseException>(() => parameters.ValidarReturnCodeErrMsg());
            parameters.Add("@ReturnCode", (short)0);
            parameters.ValidarReturnCodeErrMsg();
            parameters.Add("@ErrMsg", "Invalid success message");
            Assert.Throws<DatabaseException>(() => parameters.ValidarReturnCodeErrMsg());
        }

        [Theory]
        [InlineData(1, typeof(BusinessException))]
        [InlineData(2, typeof(DatabaseException))]
        [InlineData(42, typeof(DatabaseException))]
        public void FailureDoesNotPublishDatabaseMessage(short code, Type expectedType)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@ReturnCode", code);
            parameters.Add("@ErrMsg", "sensitive-marker");
            var exception = Record.Exception(() => parameters.ValidarReturnCodeErrMsg());
            Assert.IsType(expectedType, exception);
            Assert.DoesNotContain("sensitive-marker", exception.Message);
        }
    }
}
