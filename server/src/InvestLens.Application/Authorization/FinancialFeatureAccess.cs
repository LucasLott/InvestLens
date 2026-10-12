namespace InvestLens.Application.Authorization;

// Matriz transitória: a Etapa 9 fornecerá a assinatura efetiva e a Etapa 10
// substituirá esta decisão fixa por permissões persistidas.
public enum FinancialFeature
{
    AssetSearch,
    AssetDetails,
    CurrentIndicators,
    CurrentQuote,
    DividendHistory,
    PriceHistory,
    FundamentalHistory,
    FundamentalScreener,
    MaximumPrice
}

public enum FinancialAccessDecision { Allowed, FeatureNotAvailable }

public interface IFinancialFeatureAccess
{
    FinancialAccessDecision Evaluate(FinancialFeature feature);
}

public sealed class ConservativeFinancialFeatureAccess : IFinancialFeatureAccess
{
    public FinancialAccessDecision Evaluate(FinancialFeature feature) => feature switch
    {
        FinancialFeature.AssetSearch or FinancialFeature.AssetDetails or
        FinancialFeature.CurrentIndicators or FinancialFeature.CurrentQuote => FinancialAccessDecision.Allowed,
        _ => FinancialAccessDecision.FeatureNotAvailable
    };
}
