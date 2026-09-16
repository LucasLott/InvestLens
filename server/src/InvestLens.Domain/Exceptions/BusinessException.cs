namespace InvestLens.Domain.Exceptions
{
    public sealed class BusinessException() : Exception("A operação viola uma regra de negócio.");
}