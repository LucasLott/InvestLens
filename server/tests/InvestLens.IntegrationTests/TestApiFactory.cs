using System.Security.Cryptography;
using InvestLens.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace InvestLens.IntegrationTests
{
    public class TestApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var runBolsaiConnectivity = string.Equals(Environment.GetEnvironmentVariable("BOLSAI_RUN_CONNECTIVITY_TESTS"), "true", StringComparison.OrdinalIgnoreCase);
            builder.UseEnvironment(runBolsaiConnectivity ? "Development" : "Testing");
            var settings = new Dictionary<string, string?>
            {
                ["Authentication:Jwt:Secret"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
                ["Authentication:Jwt:Issuer"] = "InvestLens.Tests",
                ["Authentication:Jwt:Audience"] = "InvestLens.Tests.Client",
                ["Authentication:Jwt:ExpirationInMinutes"] = "17"
            };

            if (!runBolsaiConnectivity)
                settings["Bolsai:ApiKey"] = "test-only-bolsai-key";

            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));
        }
    }
}
