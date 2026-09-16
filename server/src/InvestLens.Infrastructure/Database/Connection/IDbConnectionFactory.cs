using System.Data.Common;

namespace InvestLens.Infrastructure.Database.Connection
{
    public interface IDbConnectionFactory
    {
        DbConnection CreateConnection();
    }
}