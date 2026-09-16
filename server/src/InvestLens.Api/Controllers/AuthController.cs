using InvestLens.Application.DTOs.Auth;
using InvestLens.Application.Interfaces.Services.Auth;
using Microsoft.AspNetCore.Mvc;

namespace InvestLens.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public sealed class AuthController(IAuthService authService) : ControllerBase
    {
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
        {
            return Ok(await authService.LoginAsync(request, cancellationToken));
        }
    }
}
