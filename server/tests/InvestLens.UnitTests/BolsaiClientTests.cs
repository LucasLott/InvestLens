using System.Net;
using System.Net.Http;
using InvestLens.Infrastructure.Bolsai;
using InvestLens.Infrastructure.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InvestLens.UnitTests;

public sealed class BolsaiClientTests
{
    private const string ApiKey = "test-key-must-never-appear-in-logs";

    [Fact]
    public async Task VerifyConnectivityAsync_SendsAuthenticatedGetToDocumentedEndpoint()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });
        var client = CreateClient(handler);

        await client.VerifyConnectivityAsync(CancellationToken.None);

        Assert.NotNull(handler.Request);
        Assert.Equal(HttpMethod.Get, handler.Request!.Method);
        Assert.Equal("https://api.usebolsai.com/api/v1/stocks/?bdi_code=02&limit=1", handler.Request.RequestUri!.ToString());
        Assert.True(handler.Request.Headers.TryGetValues("X-API-Key", out var values));
        Assert.Equal(ApiKey, Assert.Single(values));
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, BolsaiFailureKind.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden, BolsaiFailureKind.Forbidden)]
    [InlineData(HttpStatusCode.NotFound, BolsaiFailureKind.NotFound)]
    [InlineData(HttpStatusCode.TooManyRequests, BolsaiFailureKind.RateLimited)]
    [InlineData(HttpStatusCode.BadGateway, BolsaiFailureKind.ExternalServiceFailure)]
    public async Task VerifyConnectivityAsync_ClassifiesHttpFailures(HttpStatusCode statusCode, BolsaiFailureKind expectedKind)
    {
        var client = CreateClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(statusCode)));

        var exception = await Assert.ThrowsAsync<BolsaiRequestException>(() => client.VerifyConnectivityAsync(CancellationToken.None));

        Assert.Equal(expectedKind, exception.Kind);
        Assert.Equal(statusCode, exception.StatusCode);
        Assert.DoesNotContain(ApiKey, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task VerifyConnectivityAsync_ClassifiesNetworkFailureWithoutLeakingApiKey()
    {
        var client = CreateClient(new StubHttpMessageHandler(_ => throw new HttpRequestException(ApiKey)));

        var exception = await Assert.ThrowsAsync<BolsaiRequestException>(() => client.VerifyConnectivityAsync(CancellationToken.None));

        Assert.Equal(BolsaiFailureKind.NetworkUnavailable, exception.Kind);
        Assert.DoesNotContain(ApiKey, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task VerifyConnectivityAsync_DoesNotWriteApiKeyToLogs()
    {
        var logger = new CapturingLogger();
        var client = CreateClient(new StubHttpMessageHandler(_ => throw new HttpRequestException(ApiKey)), logger);

        await Assert.ThrowsAsync<BolsaiRequestException>(() => client.VerifyConnectivityAsync(CancellationToken.None));

        Assert.DoesNotContain(ApiKey, string.Join(' ', logger.Messages));
    }

    [Fact]
    public async Task VerifyConnectivityAsync_ClassifiesTimeout()
    {
        var client = CreateClient(new StubHttpMessageHandler(_ => throw new TaskCanceledException()));

        var exception = await Assert.ThrowsAsync<BolsaiRequestException>(() => client.VerifyConnectivityAsync(CancellationToken.None));

        Assert.Equal(BolsaiFailureKind.Timeout, exception.Kind);
    }

    [Fact]
    public async Task VerifyConnectivityAsync_PreservesCallerCancellation()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var client = CreateClient(new StubHttpMessageHandler(_ => throw new OperationCanceledException(source.Token)));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.VerifyConnectivityAsync(source.Token));
    }

    [Theory]
    [InlineData("http://api.usebolsai.com", "valid-key", 30)]
    [InlineData("https://api.usebolsai.com", "", 30)]
    [InlineData("https://api.usebolsai.com", "valid-key", 0)]
    public void SettingsValidator_RejectsUnsafeOrMissingSettings(string baseUrl, string apiKey, int timeout)
    {
        var result = new BolsaiSettingsValidator().Validate(null, new BolsaiSettings { BaseUrl = baseUrl, ApiKey = apiKey, TimeoutSeconds = timeout });

        Assert.False(result.Succeeded);
        Assert.DoesNotContain(ApiKey, string.Join(' ', result.Failures ?? []));
    }

    private static BolsaiClient CreateClient(HttpMessageHandler handler, ILogger<BolsaiClient>? logger = null) => new(
        new HttpClient(handler) { BaseAddress = new Uri("https://api.usebolsai.com/api/v1/") },
        Options.Create(new BolsaiSettings { BaseUrl = "https://api.usebolsai.com/api/v1/", ApiKey = ApiKey, TimeoutSeconds = 30 }),
        logger ?? NullLogger<BolsaiClient>.Instance);

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> send) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(send(request));
        }
    }

    private sealed class CapturingLogger : ILogger<BolsaiClient>
    {
        public List<string> Messages { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Messages.Add(formatter(state, exception));
    }
}
