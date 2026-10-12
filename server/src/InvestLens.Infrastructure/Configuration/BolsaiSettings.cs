namespace InvestLens.Infrastructure.Configuration
{
    public sealed class BolsaiSettings
    {
        public const string SectionName = "Bolsai";

        public string BaseUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; }
        public int DailyRequestLimit { get; set; } = 10000;
        public int DailyOperationalBudget { get; set; } = 9000;
        public int DailySafetyReserve { get; set; } = 1000;
    }
}
