using InvestLens.Application.Interfaces.Repositories;
using InvestLens.Application.Interfaces.Services;

namespace InvestLens.Application.Services;

public sealed class FinancialUpdateService(IFinancialUpdateRepository repository, IFinancialImportService importer, TimeProvider clock) : IFinancialUpdateService
{
    public async Task RunAsync(int batchSize, int maxRequests, TimeSpan minimumInterval, CancellationToken cancellationToken)
    {
        var tickers = await repository.GetTickersAsync(Math.Min(batchSize, maxRequests / 3), cancellationToken);
        foreach (var ticker in tickers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await importer.ImportAsync(ticker, cancellationToken);
            if (minimumInterval > TimeSpan.Zero) await Task.Delay(minimumInterval, clock, cancellationToken);
        }
    }
}
