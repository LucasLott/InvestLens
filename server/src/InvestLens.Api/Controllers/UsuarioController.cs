using InvestLens.Application.DTOs.Usuario;
using InvestLens.Application.Interfaces.Services.Usuario;
using Microsoft.AspNetCore.Mvc;

namespace InvestLens.Api.Controllers
{
    [ApiController]
    [Route("api/usuario")]
    public class UsuarioController(IUsuarioService usuarioService) : ControllerBase
    {
        private readonly IUsuarioService _usuarioService = usuarioService;

        [HttpPost]
        public async Task<IActionResult> Adicionar(AdicionarUsuarioRequest request, CancellationToken cancellationToken)
        {
            await _usuarioService.Adicionar(request, cancellationToken);

            return Ok();
        }
    }
}