using System.Globalization;
using System.Security.Claims;
using InvestLens.Application.Authentication;

namespace InvestLens.Api.Authentication
{
    internal static class AuthenticatedClaims
    {
        internal static string? GetValue(ClaimsPrincipal? usuario, string nomeClaim)
        {
            var claims = usuario?.Identities
                .Where(identity => identity.IsAuthenticated)
                .SelectMany(identity => identity.Claims)
                .Where(claim => string.Equals(claim.Type, nomeClaim, StringComparison.Ordinal))
                .Take(2).ToArray();

            // Claims duplicadas tornam a identidade ambígua, mesmo com valores iguais.
            return claims is { Length: 1 } && !string.IsNullOrWhiteSpace(claims[0].Value)
                ? claims[0].Value : null;
        }

        internal static int? GetIdUsuario(ClaimsPrincipal? usuario) =>
            ParseIdUsuario(GetValue(usuario, JwtClaimNames.IdUsuario));

        internal static int? ParseIdUsuario(string? valor) =>
            int.TryParse(valor, NumberStyles.None, CultureInfo.InvariantCulture, out var idUsuario) && idUsuario > 0
                ? idUsuario : null;
    }
}
