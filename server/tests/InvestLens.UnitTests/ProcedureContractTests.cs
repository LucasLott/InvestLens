using Dapper;
using InvestLens.Domain.Exceptions;
using InvestLens.Infrastructure.Database;
using InvestLens.Infrastructure.Database.Extensions;

namespace InvestLens.UnitTests
{
    public class ProcedureContractTests
    {
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