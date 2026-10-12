using InvestLens.Application.DTOs.Plano;
using InvestLens.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace InvestLens.Api.Controllers
{
    [ApiController]
    [Route("planos")]
    public sealed class PlanoController(IPlanoService service) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<PlanoResponse>>> Listar(CancellationToken ct)
        {
            return Ok(await service.ListarAsync(ct));
        }

        [HttpGet("{idPlano:int}")]
        public async Task<ActionResult<PlanoResponse>> Obter(int idPlano, CancellationToken ct)
        {
            if (idPlano <= 0)
            {
                return NotFound();
            }

            var plano = await service.ObterAsync(idPlano, ct);

            return plano is null ? NotFound() : Ok(plano);
        }
    }
}
