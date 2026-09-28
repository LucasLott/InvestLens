using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using InvestLens.Application.DTOs.Auth;
using InvestLens.Application.Interfaces.Repositories;
using InvestLens.Application.Interfaces.Services.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InvestLens.IntegrationTests;

public class RefreshTokenEndpointTests
{
    [Fact]
    public async Task LoginRefreshReplayAndLogoutProtectSession()
    {
        var store = new RefreshStore();
        await using var factory = CreateFactory(store);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        client.DefaultRequestHeaders.Add("Origin", "https://localhost");
        client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
        client.DefaultRequestHeaders.Add("X-InvestLens-CSRF", "1");
        using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "a@b.com", senha = "password" });
        login.EnsureSuccessStatusCode();
        var original = Cookie(login);
        var setCookie = login.Headers.GetValues("Set-Cookie").Single();
        Assert.Contains("secure", setCookie);
        Assert.Contains("httponly", setCookie);
        Assert.Contains("samesite=strict", setCookie);
        Assert.Contains("path=/api/v1/auth", setCookie);
        Assert.DoesNotContain("domain=", setCookie);
        Assert.True(login.Headers.CacheControl?.NoStore);
        Assert.DoesNotContain(original.Split('=')[1], await login.Content.ReadAsStringAsync());
        Assert.Equal(Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(original.Split('=')[1]))), store.FirstHash);

        using var refreshed = await Post(client, "refresh", original);
        refreshed.EnsureSuccessStatusCode();
        var rotated = Cookie(refreshed);
        Assert.NotEqual(original, rotated);
        Assert.Equal(setCookie.Split("expires=")[1].Split(';')[0],
            refreshed.Headers.GetValues("Set-Cookie").Single().Split("expires=")[1].Split(';')[0]);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Post(client, "refresh", original)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Post(client, "refresh", rotated)).StatusCode);

        using var secondLogin = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "a@b.com", senha = "password" });
        var second = Cookie(secondLogin);
        using var logout = await Post(client, "revoke", second);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Contains("expires=Thu, 01 Jan 1970", logout.Headers.GetValues("Set-Cookie").Single());
        Assert.Equal(HttpStatusCode.NoContent, (await Post(client, "revoke", second)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Post(client, "refresh", second)).StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("bad-token")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    public async Task MissingMalformedAndUnknownTokensAreUnauthorized(string? token)
    {
        await using var factory = CreateFactory(new());
        using var client = factory.CreateClient();
        using var response = await Post(client, "refresh", token is null ? null : "__Host-InvestLens.Refresh=" + token);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("login")]
    [InlineData("refresh")]
    [InlineData("revoke")]
    public async Task BrowserRequestsRequireCsrfHeader(string endpoint)
    {
        await using var factory = CreateFactory(new());
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Origin", "https://attacker.example");
        using var response = await client.PostAsJsonAsync("/api/v1/auth/" + endpoint, new { email = "a@b.com", senha = "password" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static WebApplicationFactory<InvestLens.Api.Program> CreateFactory(RefreshStore store) =>
        new TestApiFactory().WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAuthService>();
            services.AddSingleton<IAuthService>(new AuthStub());
            services.RemoveAll<IRefreshTokenRepository>();
            services.AddSingleton<IRefreshTokenRepository>(store);
        }));

    private static string Cookie(HttpResponseMessage response) => response.Headers.GetValues("Set-Cookie").Single().Split(';')[0];
    private static Task<HttpResponseMessage> Post(HttpClient client, string endpoint, string? cookie)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/" + endpoint);
        if (cookie is not null) request.Headers.Add("Cookie", cookie);
        return client.SendAsync(request);
    }

    private sealed class AuthStub : IAuthService
    {
        public Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new LoginResponse { IdUsuario = 7, Codigo = "007", Nome = "User", Email = request.Email });
    }
}

// HTTP tests replace persistence; database transaction guarantees require SQL Server tests.
internal sealed class RefreshStore : IRefreshTokenRepository
{
    private readonly Dictionary<string, (RefreshSession Session, bool Used)> tokens = new();
    private readonly HashSet<RefreshSession> revoked = new();
    public string? FirstHash { get; private set; }
    public Task CreateAsync(int userId, string hash, DateTime expiresAt, CancellationToken cancellationToken)
    {
        FirstHash ??= hash;
        tokens.Add(hash, (new RefreshSession { IdUsuario = userId, Codigo = "007", Nome = "User", Email = "a@b.com", Expiracao = expiresAt }, false));
        return Task.CompletedTask;
    }
    public Task<RefreshSession?> RotateAsync(string hash, string replacementHash, CancellationToken cancellationToken)
    {
        if (!tokens.TryGetValue(hash, out var entry)) return Task.FromResult<RefreshSession?>(null);
        if (entry.Used || revoked.Contains(entry.Session) || entry.Session.Expiracao <= DateTime.UtcNow)
        {
            revoked.Add(entry.Session);
            return Task.FromResult<RefreshSession?>(null);
        }
        tokens[hash] = (entry.Session, true);
        tokens.Add(replacementHash, (entry.Session, false));
        return Task.FromResult<RefreshSession?>(entry.Session);
    }
    public Task RevokeAsync(string hash, CancellationToken cancellationToken)
    {
        if (tokens.TryGetValue(hash, out var entry)) revoked.Add(entry.Session);
        return Task.CompletedTask;
    }
}
