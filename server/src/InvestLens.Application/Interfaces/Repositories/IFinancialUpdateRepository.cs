namespace InvestLens.Application.Interfaces.Repositories;

public interface IFinancialUpdateRepository
{
    Task<IReadOnlyList<string>> GetTickersAsync(int limit, CancellationToken cancellationToken);
}
