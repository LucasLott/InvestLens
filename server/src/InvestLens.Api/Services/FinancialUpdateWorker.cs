using InvestLens.Api.Configuration;
using InvestLens.Application.Interfaces.Services;
using Microsoft.Extensions.Options;

namespace InvestLens.Api.Services;

public sealed class FinancialUpdateWorker(IServiceScopeFactory scopes, IOptions<FinancialUpdateSettings> options, ILogger<FinancialUpdateWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled) { logger.LogInformation("Atualização financeira automática desabilitada."); return; }
        if (!settings.RunOnStartup) await Task.Delay(TimeSpan.FromMinutes(settings.IntervalMinutes), stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(settings.IntervalMinutes));
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IFinancialUpdateService>().RunAsync(settings.BatchSize, settings.MaxRequestsPerExecution, TimeSpan.FromSeconds(settings.MinimumIntervalBetweenRequestsSeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "Falha na atualização financeira automática."); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
