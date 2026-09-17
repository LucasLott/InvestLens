using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using InvestLens.Application.DTOs.Auth;
using InvestLens.Application.Interfaces.Security;
using InvestLens.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace InvestLens.Infrastructure.Security
{
    public sealed class JwtTokenGenerator(IOptions<JwtSettings> options,
                                          SymmetricSecurityKey signingKey,
                                          TimeProvider timeProvider) : IJwtTokenGenerator
    {
        private readonly IOptions<JwtSettings> _options = options;
        private readonly SymmetricSecurityKey _signingKey = signingKey;
        private readonly TimeProvider _timeProvider = timeProvider;

        public Token Generate(int idUsuario, string codigo, string nome)
        {
            var settings = _options.Value;

            // NumericDate no JWT tem precisão de segundos; a resposta usa o mesmo instante.
            var issuedAt = DateTimeOffset.FromUnixTimeSeconds(_timeProvider.GetUtcNow().ToUnixTimeSeconds());

            var expiration = issuedAt.AddMinutes(settings.ExpirationInMinutes).UtcDateTime;

            Claim[] claims = [
                new Claim("IdUsuario", idUsuario.ToString(CultureInfo.InvariantCulture)),
                new Claim("Codigo", codigo),
                new Claim("Nome", nome)
            ];

            var jwt = new JwtSecurityToken(issuer: settings.Issuer,
                                           audience: settings.Audience,
                                           claims: claims,
                                           notBefore: issuedAt.UtcDateTime,
                                           expires: expiration,
                                           signingCredentials: new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256));

            return new Token
            {
                AccessToken = new JwtSecurityTokenHandler().WriteToken(jwt),
                Expiration = expiration
            };
        }
    }
}
