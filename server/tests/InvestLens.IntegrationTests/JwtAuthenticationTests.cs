using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using InvestLens.Application.Interfaces.Security;
using InvestLens.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace InvestLens.IntegrationTests
{
    public class JwtAuthenticationTests
    {
        [Theory]
        [InlineData("valid", 200)]
        [InlineData("missing", 401)]
        [InlineData("malformed", 401)]
        [InlineData("wrong-key", 401)]
        [InlineData("wrong-issuer", 401)]
        [InlineData("wrong-audience", 401)]
        [InlineData("expired", 401)]
        [InlineData("future", 401)]
        [InlineData("unsigned", 401)]
        public async Task BearerMiddlewareValidatesTokensAndReturnsSafeErrors(string scenario, int expectedStatus)
        {
            await using var factory = new TestApiFactory().WithWebHostBuilder(builder =>
                builder.ConfigureServices(services => services.AddControllers().AddApplicationPart(typeof(JwtProbeController).Assembly)));
            using var client = factory.CreateClient();
            var settings = factory.Services.GetRequiredService<IOptions<JwtSettings>>().Value;
            string? token = null;
            if (scenario == "valid")
                token = factory.Services.GetRequiredService<IJwtTokenGenerator>().Generate(123, "001", "Lucas").AccessToken;
            else if (scenario == "malformed") token = "sensitive-marker";
            else if (scenario != "missing")
            {
                var key = scenario == "wrong-key"
                    ? new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32)) : settings.CreateSigningKey();
                var jwt = new JwtSecurityToken(
                    issuer: scenario == "wrong-issuer" ? "other" : settings.Issuer,
                    audience: scenario == "wrong-audience" ? "other" : settings.Audience,
                    claims: [new Claim("Nome", "sensitive-marker")],
                    notBefore: scenario == "future" ? DateTime.UtcNow.AddMinutes(1) : DateTime.UtcNow.AddMinutes(-2),
                    expires: scenario == "expired" ? DateTime.UtcNow.AddSeconds(-1) : DateTime.UtcNow.AddMinutes(5),
                    signingCredentials: scenario == "unsigned" ? null : new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
                token = new JwtSecurityTokenHandler().WriteToken(jwt);
            }
            if (token is not null) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await client.GetAsync("/tests/jwt");
            Assert.Equal(expectedStatus, (int)response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain(settings.Secret, body);
            Assert.DoesNotContain("sensitive-marker", body);
            if (token is not null) Assert.DoesNotContain(token, body);
            using var json = JsonDocument.Parse(body);
            if (expectedStatus == 200)
            {
                Assert.Equal("123", json.RootElement.GetProperty("idUsuario").GetString());
                Assert.Equal("001", json.RootElement.GetProperty("codigo").GetString());
                Assert.Equal("Lucas", json.RootElement.GetProperty("nome").GetString());
            }
            else
            {
                Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
                Assert.Equal(401, json.RootElement.GetProperty("status").GetInt32());
                Assert.True(json.RootElement.TryGetProperty("traceId", out _));
                Assert.DoesNotContain("error_description", response.Headers.WwwAuthenticate.ToString());
            }
        }

        [Theory]
        [InlineData("Secret", "")]
        [InlineData("Secret", "invalid-sensitive-marker")]
        [InlineData("Issuer", "")]
        [InlineData("Audience", "")]
        [InlineData("ExpirationInMinutes", "0")]
        public void InvalidJwtConfigurationPreventsStartupWithoutExposingSecret(string property, string value)
        {
            using var factory = new TestApiFactory().WithWebHostBuilder(builder =>
                builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                    new Dictionary<string, string?> { [$"Authentication:Jwt:{property}"] = value })));
            var exception = Assert.Throws<OptionsValidationException>(() => factory.CreateClient());
            Assert.DoesNotContain("sensitive-marker", exception.Message);
        }
    }

    // Controller disponível apenas no host de testes; endpoints de produção não são protegidos nesta tarefa.
    [ApiController]
    [Authorize]
    [Route("tests/jwt")]
    public sealed class JwtProbeController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get() => Ok(new
        {
            IdUsuario = User.FindFirst("IdUsuario")?.Value,
            Codigo = User.FindFirst("Codigo")?.Value,
            Nome = User.Identity?.Name
        });
    }
}
