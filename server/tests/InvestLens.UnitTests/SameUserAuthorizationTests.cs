using System.Security.Claims;
using InvestLens.Api.Authorization;
using InvestLens.Application.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace InvestLens.UnitTests
{
    public class SameUserAuthorizationTests
    {
        [Theory]
        [InlineData("15", "15", true)]
        [InlineData("15", "015", true)]
        [InlineData("27", "15", false)]
        [InlineData(null, "15", false)]
        [InlineData("ABC", "15", false)]
        [InlineData("15", null, false)]
        [InlineData("15", "ABC", false)]
        [InlineData("0", "0", false)]
        [InlineData("-1", "-1", false)]
        [InlineData("2147483648", "2147483648", false)]
        [InlineData("15", "2147483648", false)]
        [InlineData("15", "15.0", false)]
        [InlineData("", "15", false)]
        public async Task ComparaIdsValidosNumericamente(string? claim, string? rota, bool autorizado)
        {
            var usuario = new ClaimsPrincipal(new ClaimsIdentity(
                claim is null ? [] : [new Claim(JwtClaimNames.IdUsuario, claim)], "Bearer"));
            var httpContext = new DefaultHttpContext { User = usuario };
            if (rota is not null) httpContext.Request.RouteValues["idUsuario"] = rota;

            var requirement = new SameUserRequirement();
            var context = new AuthorizationHandlerContext([requirement], usuario, httpContext);
            await new SameUserAuthorizationHandler().HandleAsync(context);

            Assert.Equal(autorizado, context.HasSucceeded);
        }

        [Theory]
        [InlineData(false, false, true)]
        [InlineData(true, true, true)]
        [InlineData(true, false, false)]
        public async Task RejeitaUsuarioAnonimoClaimDuplicadaOuRecursoIncompativel(bool autenticado, bool duplicada, bool recursoHttp)
        {
            var claims = new List<Claim> { new(JwtClaimNames.IdUsuario, "15") };
            if (duplicada) claims.Add(new(JwtClaimNames.IdUsuario, "15"));
            var usuario = new ClaimsPrincipal(new ClaimsIdentity(claims, autenticado ? "Bearer" : null));
            var httpContext = new DefaultHttpContext { User = usuario };
            httpContext.Request.RouteValues["idUsuario"] = 15;
            var context = new AuthorizationHandlerContext([new SameUserRequirement()], usuario, recursoHttp ? httpContext : null);

            await new SameUserAuthorizationHandler().HandleAsync(context);

            Assert.False(context.HasSucceeded);
        }
    }
}
