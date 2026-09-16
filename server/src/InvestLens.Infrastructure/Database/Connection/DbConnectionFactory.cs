using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace InvestLens.Infrastructure.Database.Connection
{
    public sealed class DbConnectionFactory(IConfiguration configuration) : IDbConnectionFactory
    {
        // A configuração só é exigida sob demanda. O consumidor abre e descarta a conexão.
        public DbConnection CreateConnection()
        {
            var value = configuration.GetConnectionString("InvestLens");
            if (string.IsNullOrWhiteSpace(value))
                throw new DatabaseException("A configuração de conexão InvestLens não foi fornecida.");
            return new SqlConnection(value);
        }
    }
}