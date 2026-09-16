using InvestLens.Api.Extensions;
using InvestLens.Application;
using InvestLens.Infrastructure.DependencyInjection;

namespace InvestLens.Api
{
    public partial class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.AddApiLogging();
            builder.Services.AddApplication().AddInfrastructure().AddApiServices(builder.Configuration);
            var app = builder.Build();
            app.UseApiErrors();
            app.UseApiDocumentation();
            app.MapControllers();
            app.MapHealthChecks("/health");
            app.Services.GetRequiredService<ILogger<Program>>().LogInformation("InvestLens API inicializada.");
            app.Run();
        }
    }
}
