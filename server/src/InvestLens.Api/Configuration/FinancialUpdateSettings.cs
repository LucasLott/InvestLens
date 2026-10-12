using Microsoft.Extensions.Options;

namespace InvestLens.Api.Configuration;

public sealed class FinancialUpdateSettings
{
    public const string SectionName = "FinancialUpdate";
    public bool Enabled { get; init; }
    public bool RunOnStartup { get; init; }
    public int IntervalMinutes { get; init; } = 60;
    public int BatchSize { get; init; } = 10;
    public int MaxRequestsPerExecution { get; init; } = 30;
    public int MinimumIntervalBetweenRequestsSeconds { get; init; } = 2;
}

public sealed class FinancialUpdateSettingsValidator : IValidateOptions<FinancialUpdateSettings>
{
    public ValidateOptionsResult Validate(string? name, FinancialUpdateSettings value) =>
        value.IntervalMinutes <= 0 || value.BatchSize <= 0 || value.MaxRequestsPerExecution < 3 || value.MinimumIntervalBetweenRequestsSeconds < 0
            ? ValidateOptionsResult.Fail("FinancialUpdate possui valores inválidos.") : ValidateOptionsResult.Success;
}
