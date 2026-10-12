namespace InvestLens.Application.Interfaces.Services;

public interface IFinancialUpdateService
{
    Task RunAsync(int batchSize, int maxRequests, TimeSpan minimumInterval, CancellationToken cancellationToken);
}
