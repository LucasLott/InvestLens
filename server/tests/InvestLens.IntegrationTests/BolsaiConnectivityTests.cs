using InvestLens.Infrastructure.Bolsai;
using Microsoft.Extensions.DependencyInjection;

namespace InvestLens.IntegrationTests;

[Trait("Category", "BolsaiConnectivity")]
public sealed class BolsaiConnectivityTests(TestApiFactory factory) : IClassFixture<TestApiFactory>
{
    [Fact]
    public async Task VerifyConnectivityAsync_UsesOneRealAuthenticatedRequest_WhenExplicitlyEnabled()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("BOLSAI_RUN_CONNECTIVITY_TESTS"), "true", StringComparison.OrdinalIgnoreCase))
            return;

        var client = factory.Services.GetRequiredService<BolsaiClient>();
        foreach (var ticker in new[] { "PETR4", "VALE3", "ITUB4" })
        {
            var snapshot = await client.GetCompanySnapshotAsync(ticker, CancellationToken.None);
            Assert.Equal(ticker, snapshot.QueriedTicker);
        }
    }
}
