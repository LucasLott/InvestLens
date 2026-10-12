namespace InvestLens.Application.Models;

public sealed record FinancialImportResult(string Ticker, DateTime ImportedAtUtc, string Message);

