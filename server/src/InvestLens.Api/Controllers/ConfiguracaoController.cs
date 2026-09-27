using InvestLens.Api.Authorization;
using InvestLens.Application.DTOs.Configuracao;
using InvestLens.Application.Interfaces.Services.Configuracao;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvestLens.Api.Controllers
{
    [ApiController]
    [Route("api/usuarios/{idUsuario:int}/configuracao")]
    [Authorize(Policy = AuthorizationPolicies.SameUser)]
    public sealed class ConfiguracaoController(IConfiguracaoService configuracaoService) : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType(typeof(ConfiguracaoResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ConfiguracaoResponse>> Obter(int idUsuario, CancellationToken cancellationToken) =>
            Ok(await configuracaoService.ObterAsync(idUsuario, cancellationToken));

        [HttpPost]
        [ProducesResponseType(typeof(ConfiguracaoResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ConfiguracaoResponse>> Adicionar(int idUsuario, ConfiguracaoRequest request, CancellationToken cancellationToken)
        {
            var response = await configuracaoService.AdicionarAsync(idUsuario, request, cancellationToken);
            return CreatedAtAction(nameof(Obter), new { idUsuario }, response);
        }

        [HttpPut]
        [ProducesResponseType(typeof(ConfiguracaoResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ConfiguracaoResponse>> Alterar(int idUsuario, ConfiguracaoRequest request, CancellationToken cancellationToken) =>
            Ok(await configuracaoService.AlterarAsync(idUsuario, request, cancellationToken));
    }
}
