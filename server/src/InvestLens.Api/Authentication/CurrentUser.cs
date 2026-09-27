using InvestLens.Application.Authentication;
using InvestLens.Application.Interfaces.Authentication;

namespace InvestLens.Api.Authentication
{
    public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
    {
        public int? IdUsuario => AuthenticatedClaims.GetIdUsuario(httpContextAccessor.HttpContext?.User);
        public string? Codigo => AuthenticatedClaims.GetValue(httpContextAccessor.HttpContext?.User, JwtClaimNames.Codigo);
        public string? Nome => AuthenticatedClaims.GetValue(httpContextAccessor.HttpContext?.User, JwtClaimNames.Nome);
    }
}
