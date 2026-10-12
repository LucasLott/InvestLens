using InvestLens.Application.Models;

namespace InvestLens.Application.Interfaces.Services;

public interface IFinancialImportService
{
    Task<FinancialImportResult> ImportAsync(string ticker, CancellationToken cancellationToken);
}
