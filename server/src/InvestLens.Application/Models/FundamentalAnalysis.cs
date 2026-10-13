using InvestLens.Application.DTOs.Configuracao;

namespace InvestLens.Application.Models
{
    public enum CriterionStatus { Approved, Rejected, InsufficientData }
    public enum AssetAnalysisStatus { Approved, Rejected, InsufficientData }
    public enum MaximumPriceStatus { Available, InsufficientData, InvalidReturn }

    public sealed record FundamentalIndicators(string Ticker, int CompanyId, DateOnly? ReferenceDate, DateTime AnalyzedAtUtc, decimal? PriceToEarnings, decimal? DividendYield, decimal? ReturnOnEquity, decimal? DebtToEquity, decimal? NetMargin, decimal? CurrentPrice);
    public sealed record CriterionResult(string Code, string Name, decimal? Actual, decimal Required, string Operator, CriterionStatus Status, string Reason);
    public sealed record MaximumPriceResult(decimal? Value, decimal ReturnPercent, decimal? AverageAnnualDividend, int YearsUsed, MaximumPriceStatus Status, string Reason);
    public sealed record FundamentalAnalysisResult(FundamentalIndicators Indicators, IReadOnlyList<CriterionResult> Criteria, AssetAnalysisStatus Status, MaximumPriceResult MaximumPrice, decimal? PriceDifferencePercent);

    public static class FundamentalAnalysisEngine
    {
        public static FundamentalAnalysisResult Analyze(FundamentalIndicators i, ConfiguracaoResponse c, decimal? averageAnnualDividend = null, int yearsUsed = 0)
        {
            var criteria = new[] { Check("PL", "P/L", i.PriceToEarnings, c.PLMaximo, "<", (a, b) => a < b), Check("DY", "Dividend Yield", i.DividendYield, c.DYMinimo, ">", (a, b) => a > b), Check("ROE", "ROE", i.ReturnOnEquity, c.ROEMinimo, ">", (a, b) => a > b), Check("DIVIDA_PATRIMONIO", "Dívida/Patrimônio", i.DebtToEquity, c.DividaPatrimonioMaximo, "<", (a, b) => a < b), Check("MARGEM_LIQUIDA", "Margem líquida", i.NetMargin, c.MargemLiquidaMinimo, ">", (a, b) => a > b) };
            var status = criteria.Any(x => x.Status == CriterionStatus.Rejected) ? AssetAnalysisStatus.Rejected : criteria.Any(x => x.Status == CriterionStatus.InsufficientData) ? AssetAnalysisStatus.InsufficientData : AssetAnalysisStatus.Approved;
            var maximum = Maximum(c.RetornoPreco, averageAnnualDividend, yearsUsed);
            decimal? difference = maximum.Value is not null && i.CurrentPrice is > 0 ? (maximum.Value.Value - i.CurrentPrice.Value) / i.CurrentPrice.Value * 100m : null;
            return new(i, criteria, status, maximum, difference);
        }
        
        private static CriterionResult Check(string code, string name, decimal? actual, decimal required, string op, Func<decimal, decimal, bool> predicate) => actual is null ? new(code, name, null, required, op, CriterionStatus.InsufficientData, "Indicador indisponível.") : predicate(actual.Value, required) ? new(code, name, actual, required, op, CriterionStatus.Approved, "Critério atendido.") : new(code, name, actual, required, op, CriterionStatus.Rejected, "Critério não atendido.");
        private static MaximumPriceResult Maximum(decimal rate, decimal? dividend, int years) => rate <= 0 ? new(null, rate, dividend, years, MaximumPriceStatus.InvalidReturn, "Retorno desejado deve ser positivo.") : dividend is null || years < 5 ? new(null, rate, dividend, years, MaximumPriceStatus.InsufficientData, "Histórico confiável de cinco anos indisponível.") : new(dividend.Value / (rate / 100m), rate, dividend, years, MaximumPriceStatus.Available, "Média anual de proventos por ação.");
    }
}