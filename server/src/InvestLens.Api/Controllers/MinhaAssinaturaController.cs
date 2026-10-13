using InvestLens.Application.DTOs.Assinatura;
using InvestLens.Application.Interfaces.Authentication;
using InvestLens.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvestLens.Api.Controllers
{

    [ApiController]
    [Authorize]
    [Route("minha-assinatura")]
    public sealed class MinhaAssinaturaController(IAssinaturaService service, ICurrentUser currentUser) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<MinhaAssinaturaResponse>> Obter(CancellationToken cancellationToken)
        {
            if (currentUser.IdUsuario is not int idUsuario) return Unauthorized();
            
            return Ok(await service.ObterMinhaAsync(idUsuario, cancellationToken));
        }
    }
}