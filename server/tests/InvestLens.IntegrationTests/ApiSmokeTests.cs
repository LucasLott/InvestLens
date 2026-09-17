using System.Net;
using System.Text.Json;
using InvestLens.Api;
using InvestLens.Infrastructure.Database;
using InvestLens.Infrastructure.Database.Connection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InvestLens.IntegrationTests
{
    public class ApiSmokeTests
    {
        [Fact]
        public async Task ApiStartsWithoutDatabaseAndExposesHealthSwaggerAndProblemDetails()
        {
            await using var factory = new TestApiFactory().WithWebHostBuilder(builder =>
                builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:InvestLens"] = "",
                        ["Swagger:EnableSwaggerProd"] = "true"
                    })));
            using var client = factory.CreateClient();
            var connectionFactory = factory.Services.GetRequiredService<IDbConnectionFactory>();
            Assert.Throws<DatabaseException>(() => connectionFactory.CreateConnection());

            var health = await client.GetAsync("/health");
            Assert.Equal(HttpStatusCode.OK, health.StatusCode);
            Assert.Equal("Healthy", await health.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/swagger/index.html")).StatusCode);
            using var document = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
            Assert.Equal("InvestLens API", document.RootElement.GetProperty("info").GetProperty("title").GetString());
            Assert.Equal("v1", document.RootElement.GetProperty("info").GetProperty("version").GetString());
            var missing = await client.GetAsync("/missing");
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
            Assert.Equal("application/problem+json", missing.Content.Headers.ContentType?.MediaType);
            using var problem = JsonDocument.Parse(await missing.Content.ReadAsStringAsync());
            Assert.False(string.IsNullOrWhiteSpace(problem.RootElement.GetProperty("traceId").GetString()));
        }
    }
}
