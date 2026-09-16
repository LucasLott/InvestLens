using System.Net;
using System.Text.Json;
using InvestLens.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace InvestLens.IntegrationTests
{
    public class SwaggerConfigurationTests
    {
        [Theory]
        [InlineData(false, "Production", "v1")]
        [InlineData(true, "Production", "custom-version")]
        [InlineData(false, "Development", "v1")]
        [InlineData(true, "Development", "custom-version")]
        public async Task SwaggerUsesConfiguredMetadataAndBuildMode(bool enableSwaggerProd, string environment, string version)
        {
            await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(environment);
                builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Swagger:Title"] = "Configured title",
                        ["Swagger:Version"] = version,
                        ["Swagger:Description"] = "Configured description",
                        ["Swagger:EnableSwaggerProd"] = enableSwaggerProd.ToString()
                    }));
            });
            using var client = factory.CreateClient();
            var ui = await client.GetAsync("/swagger");
            var document = await client.GetAsync($"/swagger/{version}/swagger.json");
#if DEBUG
            var enabled = true;
#else
            var enabled = enableSwaggerProd;
#endif
            var expectedStatus = enabled ? HttpStatusCode.OK : HttpStatusCode.NotFound;
            Assert.Equal(expectedStatus, ui.StatusCode);
            Assert.Equal(expectedStatus, document.StatusCode);
            if (!enabled) return;

            using var json = JsonDocument.Parse(await document.Content.ReadAsStringAsync());
            var info = json.RootElement.GetProperty("info");
            Assert.Equal("Configured title", info.GetProperty("title").GetString());
            Assert.Equal(version, info.GetProperty("version").GetString());
            Assert.Equal("Configured description", info.GetProperty("description").GetString());
            Assert.Contains("Configured title", await ui.Content.ReadAsStringAsync());
        }
    }
}
