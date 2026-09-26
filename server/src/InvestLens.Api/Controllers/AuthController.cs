namespace InvestLens.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public sealed class AuthController(IAuthService authService, RefreshTokenService refreshTokens) : ControllerBase
    {
        private const string CookieName = "__Host-InvestLens.Refresh";

        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
        {
            CheckBrowserRequest();

            var response = await authService.LoginAsync(request, cancellationToken);

            await refreshTokens.RevokeAsync(Request.Cookies[CookieName], cancellationToken);

            SetCookie(await refreshTokens.CreateAsync(response.IdUsuario, cancellationToken));

            return Ok(response);
        }

        [HttpPost("refresh")]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<LoginResponse>> Refresh(CancellationToken cancellationToken)
        {
            CheckBrowserRequest();
            try
            {
                var result = await refreshTokens.RefreshAsync(Request.Cookies[CookieName], cancellationToken);
                SetCookie(result.Cookie);
                return Ok(result.Response);
            }
            catch (CredenciaisInvalidasException)
            {
                DeleteCookie();
                throw;
            }
        }

        [HttpPost("revoke")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Revoke(CancellationToken cancellationToken)
        {
            CheckBrowserRequest();
            await refreshTokens.RevokeAsync(Request.Cookies[CookieName], cancellationToken);
            DeleteCookie();
            return NoContent();
        }

        private void CheckBrowserRequest()
        {
            Response.Headers.CacheControl = "no-store";
            Response.Headers.Pragma = "no-cache";
            // Custom header cannot be added by a cross-origin HTML form. No permissive CORS policy.
            // SameSite alone does not block malicious sibling origins on the same site.
            if (Request.Headers.ContainsKey("Origin") || Request.Headers.ContainsKey("Sec-Fetch-Site"))
                if (Request.Headers["X-InvestLens-CSRF"] != "1" ||
                    Request.Headers["Sec-Fetch-Site"] == "cross-site")
                    throw new BadHttpRequestException("Invalid browser request.", StatusCodes.Status403Forbidden);
        }

        private static CookieOptions CookieOptions() => new()
        {
            HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict,
            Path = "/", IsEssential = true
        };

        private void SetCookie(RefreshCookie cookie)
        {
            var options = CookieOptions();
            options.Expires = new DateTimeOffset(cookie.ExpiresAt);
            Response.Cookies.Append(CookieName, cookie.Value, options);
        }

        private void DeleteCookie() => Response.Cookies.Delete(CookieName, CookieOptions());
    }
}
