using InvestLens.Application.Interfaces.Providers;
using InvestLens.Application.Interfaces.Repositories;
using InvestLens.Application.Interfaces.Services;
using InvestLens.Application.Models;

namespace InvestLens.Application.Services;

public sealed class FinancialImportService(ICompanyMarketDataProvider provider, IFinancialImportRepository repository, TimeProvider clock) : IFinancialImportService
{
    public async Task<FinancialImportResult> ImportAsync(string ticker, CancellationToken cancellationToken)
    {
        var snapshot = await provider.GetCompanySnapshotAsync(ticker, cancellationToken);
        await repository.PersistAsync(snapshot, cancellationToken);
        var importedAt = clock.GetUtcNow().UtcDateTime;
        return new FinancialImportResult(snapshot.QueriedTicker, importedAt, "Importação concluída.");
    }
}
