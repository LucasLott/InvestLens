using InvestLens.Application.Models;

namespace InvestLens.Application.Interfaces.Providers;

public interface ICompanyMarketDataProvider
{
    Task<CompanyMarketSnapshot> GetCompanySnapshotAsync(string ticker, CancellationToken cancellationToken);
}
