namespace InvestLens.Application.Models
{
    public sealed record CompanyMarketSnapshot(string QueriedTicker,
                                               string? PrimaryTicker,
                                               string? CorporateName,
                                               string? TradeName,
                                               string? Sector,
                                               DateOnly? QuoteDate,
                                               decimal? ClosePrice,
                                               DateOnly? FundamentalsReferenceDate,
                                               decimal? PriceToEarnings,
                                               decimal? DividendYield,
                                               decimal? ReturnOnEquity,
                                               decimal? DebtToEquity,
                                               decimal? NetMargin,
                                               IReadOnlyList<DividendPayment> Dividends);

    public sealed record DividendPayment(DateOnly? ExDate,
                                         DateOnly? PaymentDate,
                                         string? Type,
                                         decimal? ValuePerShare);
}