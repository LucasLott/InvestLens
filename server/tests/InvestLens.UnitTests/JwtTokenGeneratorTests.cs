using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using InvestLens.Infrastructure.Configuration;
using InvestLens.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace InvestLens.UnitTests
{
    public class JwtTokenGeneratorTests
    {
        [Fact]
        public void TokenHasValidSignatureClaimsAndExactUtcExpiration()
        {
            var settings = Settings();
            var now = DateTimeOffset.UtcNow;
            var generator = new JwtTokenGenerator(Options.Create(settings), settings.CreateSigningKey(), new FixedTime(now));
            var result = generator.Generate(123, "001", "Lucas");
            Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
            var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
            var validation = Validation(settings);
            var principal = handler.ValidateToken(result.AccessToken, validation, out var token);
            var jwt = Assert.IsType<JwtSecurityToken>(token);
            Assert.Equal(SecurityAlgorithms.HmacSha256, jwt.Header.Alg);
            Assert.Equal(settings.Issuer, jwt.Issuer);
            Assert.Equal(settings.Audience, Assert.Single(jwt.Audiences));
            Assert.Equal("123", principal.FindFirst("IdUsuario")?.Value);
            Assert.Equal("001", principal.FindFirst("Codigo")?.Value);
            Assert.Equal("Lucas", principal.FindFirst("Nome")?.Value);
            Assert.Equal(new[] { "Codigo", "IdUsuario", "Nome", "aud", "exp", "iss", "nbf" }, jwt.Payload.Keys.Order(StringComparer.Ordinal).ToArray());
            Assert.Equal(DateTimeKind.Utc, result.Expiration.Kind);
            Assert.Equal(jwt.ValidTo, result.Expiration);
            Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(now.ToUnixTimeSeconds()).AddMinutes(17).UtcDateTime, result.Expiration);
            validation.IssuerSigningKey = Settings().CreateSigningKey();
            Assert.ThrowsAny<SecurityTokenException>(() => handler.ValidateToken(result.AccessToken, validation, out _));
        }

        [Theory]
        [InlineData("")]
        [InlineData("not-base64-sensitive-marker")]
        [InlineData("YQ==")]
        public void InvalidSecretFailsWithoutExposingItsValue(string secret)
        {
            var settings = Settings();
            settings.Secret = secret;
            var validation = new JwtSettingsValidator().Validate(null, settings);
            Assert.True(validation.Failed);
            Assert.DoesNotContain("sensitive-marker", validation.FailureMessage);
            Assert.Throws<OptionsValidationException>(() => settings.CreateSigningKey());
        }

        [Theory]
        [InlineData(31)]
        [InlineData(33)]
        public void SecretMustRepresentExactly32Bytes(int length)
        {
            var settings = Settings();
            settings.Secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(length));
            Assert.True(new JwtSettingsValidator().Validate(null, settings).Failed);
        }

        [Theory]
        [InlineData("", "audience", 1)]
        [InlineData("issuer", " ", 1)]
        [InlineData("issuer", "audience", 0)]
        [InlineData("issuer", "audience", -1)]
        public void ConfigurationRequiresIssuerAudienceAndPositiveExpiration(string issuer, string audience, int minutes)
        {
            var settings = Settings();
            settings.Issuer = issuer;
            settings.Audience = audience;
            settings.ExpirationInMinutes = minutes;
            Assert.True(new JwtSettingsValidator().Validate(null, settings).Failed);
        }

        private static JwtSettings Settings() => new()
        {
            Secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            Issuer = "tests", Audience = "tests-client", ExpirationInMinutes = 17
        };

        private static TokenValidationParameters Validation(JwtSettings settings) => new()
        {
            ValidateIssuer = true, ValidIssuer = settings.Issuer,
            ValidateAudience = true, ValidAudience = settings.Audience,
            ValidateIssuerSigningKey = true, IssuerSigningKey = settings.CreateSigningKey(),
            ValidateLifetime = true, ClockSkew = TimeSpan.Zero,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
        };

        private sealed class FixedTime(DateTimeOffset now) : TimeProvider
        {
            public override DateTimeOffset GetUtcNow() => now;
        }
    }
}
