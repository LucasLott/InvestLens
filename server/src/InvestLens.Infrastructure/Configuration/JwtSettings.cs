using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace InvestLens.Infrastructure.Configuration
{
    public sealed class JwtSettings
    {
        public const string SectionName = "Authentication:Jwt";
        public string Secret { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpirationInMinutes { get; set; }

        public SymmetricSecurityKey CreateSigningKey()
        {
            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(Secret ?? string.Empty);
            }
            catch (FormatException)
            {
                throw InvalidSecret();
            }
            if (bytes.Length != 32) throw InvalidSecret();
            return new SymmetricSecurityKey(bytes);
        }

        private static OptionsValidationException InvalidSecret() => new(
            Options.DefaultName, typeof(JwtSettings),
            ["Authentication:Jwt:Secret deve ser Base64 válido representando 32 bytes."]);
    }
}
