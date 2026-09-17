using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using InvestLens.Api;
using InvestLens.Application.DTOs.Usuario;
using InvestLens.Application.Interfaces.Repositories.Usuario;
using InvestLens.Application.Models;
using InvestLens.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InvestLens.IntegrationTests
{
    public class LoginEndpointTests
    {
        [Theory]
        [InlineData("user@example.com", "correct-password", true, false, 200)]
        [InlineData("user@example.com", "wrong-password", true, false, 401)]
        [InlineData("missing@example.com", "correct-password", true, false, 401)]
        [InlineData("user@example.com", "correct-password", false, false, 401)]
        [InlineData("invalid-email", "correct-password", true, false, 400)]
        [InlineData("user@example.com", "", true, false, 400)]
        [InlineData("user@example.com", "correct-password", true, true, 500)]
        public async Task LoginUsesValidationRealArgon2AndSafeHttpResponses(
            string email, string password, bool active, bool failure, int expectedStatus)
        {
            var repository = new UsuarioDouble(active, failure);
            await using var factory = CreateFactory(repository);
            using var client = factory.CreateClient();
            using var response = await client.PostAsJsonAsync("/api/auth/login", new { email, senha = password });
            Assert.Equal(expectedStatus, (int)response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("correct-password", body);
            Assert.DoesNotContain("wrong-password", body);
            Assert.DoesNotContain("$argon2id", body);
            Assert.DoesNotContain("sensitive-marker", body);
            using var json = JsonDocument.Parse(body);
            if (expectedStatus == 200)
            {
                Assert.Equal(new[] { "email", "nome", "token" },
                    json.RootElement.EnumerateObject().Select(x => x.Name).Order().ToArray());
                Assert.Equal("Usuário", json.RootElement.GetProperty("nome").GetString());
                Assert.Equal(email, json.RootElement.GetProperty("email").GetString());
                var token = json.RootElement.GetProperty("token");
                var accessToken = token.GetProperty("accessToken").GetString();
                Assert.False(string.IsNullOrWhiteSpace(accessToken));
                var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
                Assert.Equal(jwt.ValidTo, token.GetProperty("expiration").GetDateTime());
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/tests/jwt")).StatusCode);
            }
            else
            {
                Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
                Assert.Equal(expectedStatus, json.RootElement.GetProperty("status").GetInt32());
                Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("traceId").GetString()));
                if (expectedStatus == 401)
                    Assert.Equal("Credenciais inválidas.", json.RootElement.GetProperty("title").GetString());
                if (expectedStatus == 400) Assert.Equal(0, repository.Calls);
            }
        }

        [Fact]
        public async Task SwaggerDescribesLoginAndSafeResponseSchema()
        {
            await using var factory = CreateFactory(new UsuarioDouble(true, false));
            using var client = factory.CreateClient();
            using var json = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
            var operation = json.RootElement.GetProperty("paths").GetProperty("/api/auth/login").GetProperty("post");
            var bearer = json.RootElement.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
            Assert.Equal("http", bearer.GetProperty("type").GetString());
            Assert.Equal("bearer", bearer.GetProperty("scheme").GetString());
            Assert.True(json.RootElement.GetProperty("security")[0].TryGetProperty("Bearer", out _));
            foreach (var status in new[] { "200", "400", "401", "500" })
                Assert.True(operation.GetProperty("responses").TryGetProperty(status, out _));
            var properties = json.RootElement.GetProperty("components").GetProperty("schemas")
                .GetProperty("LoginResponse").GetProperty("properties");
            Assert.Equal(new[] { "email", "nome", "token" },
                properties.EnumerateObject().Select(x => x.Name).Order().ToArray());
        }

        private static WebApplicationFactory<Program> CreateFactory(UsuarioDouble repository) =>
            new TestApiFactory().WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                    new Dictionary<string, string?> { ["Swagger:EnableSwaggerProd"] = "true" }));
                builder.ConfigureServices(services =>
                {
                    services.AddControllers().AddApplicationPart(typeof(JwtProbeController).Assembly);
                    services.RemoveAll<IUsuarioRepository>();
                    services.AddSingleton<IUsuarioRepository>(repository);
                });
            });

        private sealed class UsuarioDouble(bool active, bool failure) : IUsuarioRepository
        {
            private static readonly Task<string> PasswordHash = new PasswordHasher().Hash("correct-password");
            public int Calls { get; private set; }
            public async Task<DadosLogin?> ObterDadosLoginAsync(string email, CancellationToken cancellationToken)
            {
                Calls++;
                cancellationToken.ThrowIfCancellationRequested();
                if (failure) throw new InvalidOperationException("sensitive-marker");
                return email == "user@example.com" ? new DadosLogin
                {
                    IdUsuario = 7, Codigo = "007", Nome = "Usuário", Email = email,
                    SenhaHash = await PasswordHash, Ativo = active
                } : null;
            }
            public Task Adicionar(AdicionarUsuarioRequest request, string senhaHash, CancellationToken cancellationToken) =>
                throw new NotSupportedException();
        }
    }
}
