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
    public sealed class MinhaAssinaturaController(IAssinaturaService service, ISolicitacaoService solicitacoes, ICurrentUser currentUser) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<MinhaAssinaturaResponse>> Obter(CancellationToken cancellationToken)
        {
            if (currentUser.IdUsuario is not int idUsuario) return Unauthorized();
            
            return Ok(await service.ObterMinhaAsync(idUsuario, cancellationToken));
        }

        [HttpGet("solicitacoes")]
        public async Task<ActionResult<IReadOnlyList<SolicitacaoResponse>>> ListarSolicitacoes(CancellationToken cancellationToken)
        {
            if (currentUser.IdUsuario is not int idUsuario) return Unauthorized();
            return Ok(await solicitacoes.ListarAsync(idUsuario, cancellationToken));
        }

        [HttpPost("alteracoes")]
        public async Task<ActionResult<SolicitacaoResponse>> Alterar(AlteracaoPlanoRequest request, CancellationToken cancellationToken)
        {
            if (currentUser.IdUsuario is not int idUsuario) return Unauthorized();
            return Ok(await solicitacoes.AlterarAsync(idUsuario, request.IdPlanoDestino, cancellationToken));
        }

        [HttpPost("cancelamento")]
        public async Task<ActionResult<SolicitacaoResponse>> Cancelar(CancellationToken cancellationToken)
        {
            if (currentUser.IdUsuario is not int idUsuario) return Unauthorized();
            return Ok(await solicitacoes.CancelarAsync(idUsuario, cancellationToken));
        }
    }
}
