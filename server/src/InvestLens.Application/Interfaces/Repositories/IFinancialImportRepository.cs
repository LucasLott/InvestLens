using InvestLens.Application.Models;

namespace InvestLens.Application.Interfaces.Repositories;

public interface IFinancialImportRepository
{
    Task PersistAsync(CompanyMarketSnapshot snapshot, CancellationToken cancellationToken);
}
