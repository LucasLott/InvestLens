using InvestLens.Application.DTOs.Funcionalidade;
using InvestLens.Application.Interfaces.Authentication;
using InvestLens.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvestLens.Api.Controllers
{
    [ApiController]
    [Route("")]
    public sealed class FuncionalidadesController(IFuncionalidadeService service,
                                                  ICurrentUser currentUser) : ControllerBase
    {
        [AllowAnonymous]
        [HttpGet("funcionalidades")]
        public async Task<ActionResult<IReadOnlyList<FuncionalidadeResponse>>> Listar(CancellationToken cancellationToken)
        {
            return Ok(await service.ListarAsync(null, cancellationToken));
        }

        [Authorize]
        [HttpGet("minhas-funcionalidades")]
        public async Task<ActionResult<IReadOnlyList<FuncionalidadeResponse>>> Minhas(CancellationToken cancellationToken)
        {
            if (currentUser.IdUsuario is not int idUsuario) return Unauthorized();
            
            return Ok(await service.ListarAsync(idUsuario, cancellationToken));
        }
    }
}