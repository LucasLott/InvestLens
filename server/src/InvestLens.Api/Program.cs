using InvestLens.Api.Extensions;
using InvestLens.Application;
using InvestLens.Infrastructure.DependencyInjection;
using InvestLens.Api.Configuration;
using InvestLens.Api.Services;

namespace InvestLens.Api
{
    public partial class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.AddApiLogging();
            builder.Services.AddApplication().AddInfrastructure(builder.Configuration).AddApiServices(builder.Configuration);
            builder.Services.AddOptions<FinancialUpdateSettings>().Bind(builder.Configuration.GetSection(FinancialUpdateSettings.SectionName)).ValidateOnStart();
            builder.Services.AddSingleton<Microsoft.Extensions.Options.IValidateOptions<FinancialUpdateSettings>, FinancialUpdateSettingsValidator>();
            builder.Services.AddHostedService<FinancialUpdateWorker>();
            builder.Services.AddJwtAuthentication(builder.Configuration);
            var app = builder.Build();
            app.UseApiErrors();
            app.UseApiDocumentation();
            app.UseCors("InvestLensClient");
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.MapHealthChecks("/health");
            app.Services.GetRequiredService<ILogger<Program>>().LogInformation("InvestLens API inicializada.");
            app.Run();
        }
    }
}
