using InvestLens.Application.Interfaces.Authentication;
using InvestLens.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;

namespace InvestLens.Api.Authorization
{
    public sealed record FuncionalidadeRequirement(string Chave) : IAuthorizationRequirement;

    public sealed class FuncionalidadeAuthorizationHandler(IFuncionalidadeService service,
                                                           ICurrentUser currentUser,
                                                           IHttpContextAccessor httpContextAccessor,
                                                           ILogger<FuncionalidadeAuthorizationHandler> logger) : AuthorizationHandler<FuncionalidadeRequirement>
    {
        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
                                                             FuncionalidadeRequirement requirement)
        {
            if (!context.User.Identity?.IsAuthenticated ?? true)
                return;
                
            if (currentUser.IdUsuario is not int idUsuario)
                return;

            try
            {
                if (await service.PossuiPermissaoAsync(idUsuario, requirement.Chave, httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None))
                    context.Succeed(requirement);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogWarning("Falha ao avaliar funcionalidade: {Tipo}", exception.GetType().Name);
            }
        }
    }
}