namespace InvestLens.Infrastructure.Database
{
    public sealed class DatabaseException(string message, Exception? innerException = null) : Exception(message, innerException);
}