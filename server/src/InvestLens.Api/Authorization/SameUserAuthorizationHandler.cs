using System.Globalization;
using InvestLens.Api.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace InvestLens.Api.Authorization
{
    public sealed class SameUserAuthorizationHandler : AuthorizationHandler<SameUserRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, SameUserRequirement requirement)
        {
            if (context.Resource is not HttpContext httpContext)
                return Task.CompletedTask;

            var idUsuario = AuthenticatedClaims.GetIdUsuario(context.User);
            var idUsuarioRota = AuthenticatedClaims.ParseIdUsuario(
                Convert.ToString(httpContext.Request.RouteValues["idUsuario"], CultureInfo.InvariantCulture));

            if (idUsuario.HasValue && idUsuarioRota.HasValue && idUsuario == idUsuarioRota)
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }
}
