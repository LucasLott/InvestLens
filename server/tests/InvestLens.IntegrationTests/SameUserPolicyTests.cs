using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using InvestLens.Api.Authorization;
using InvestLens.Application.Authentication;
using InvestLens.Application.Interfaces.Authentication;
using InvestLens.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace InvestLens.IntegrationTests
{
    public class SameUserPolicyTests
    {
        [Theory]
        [InlineData("proprio", 200)]
        [InlineData("outro", 403)]
        [InlineData("sem-claim", 403)]
        [InlineData("claim-invalida", 403)]
        [InlineData("sem-rota", 403)]
        [InlineData("rota-invalida", 403)]
        [InlineData("sem-token", 401)]
        [InlineData("token-invalido", 401)]
        [InlineData("expirado", 401)]
        public async Task PolicyUsaBearerRealEPreservaChallengeEForbid(string cenario, int statusEsperado)
        {
            await using var factory = new TestApiFactory();
            using var scope = factory.Services.CreateScope();
            var services = scope.ServiceProvider;
            var httpContext = new DefaultHttpContext { RequestServices = services };
            if (cenario != "sem-rota") httpContext.Request.RouteValues["idUsuario"] = cenario == "rota-invalida" ? "ABC" : "15";

            var settings = services.GetRequiredService<IOptions<JwtSettings>>().Value;
            var claims = new List<Claim> { new(JwtClaimNames.Codigo, "015"), new(JwtClaimNames.Nome, "Usuário") };
            if (cenario != "sem-claim") claims.Add(new(JwtClaimNames.IdUsuario,
                cenario == "outro" ? "27" : cenario == "claim-invalida" ? "ABC" : "15"));
            if (cenario != "sem-token")
            {
                var jwt = new JwtSecurityToken(settings.Issuer, settings.Audience, claims,
                    DateTime.UtcNow.AddMinutes(-10),
                    cenario == "expirado" ? DateTime.UtcNow.AddMinutes(-1) : DateTime.UtcNow.AddMinutes(5),
                    new SigningCredentials(settings.CreateSigningKey(), SecurityAlgorithms.HmacSha256));
                httpContext.Request.Headers.Authorization = "Bearer " +
                    (cenario == "token-invalido" ? "invalid-token" : new JwtSecurityTokenHandler().WriteToken(jwt));
            }

            // Executa os serviços reais do pipeline sem registrar um endpoint de teste.
            var policy = await services.GetRequiredService<IAuthorizationPolicyProvider>().GetPolicyAsync(AuthorizationPolicies.SameUser);
            Assert.NotNull(policy);
            var evaluator = services.GetRequiredService<IPolicyEvaluator>();
            var authentication = await evaluator.AuthenticateAsync(policy, httpContext);
            var result = await evaluator.AuthorizeAsync(policy, authentication, httpContext, httpContext);
            var executou = false;
            await services.GetRequiredService<IAuthorizationMiddlewareResultHandler>().HandleAsync(
                _ => { executou = true; return Task.CompletedTask; }, httpContext, policy, result);

            Assert.Equal(statusEsperado, httpContext.Response.StatusCode);
            Assert.Equal(statusEsperado == 200, executou);
            Assert.Equal(statusEsperado == 401, result.Challenged);
            Assert.Equal(statusEsperado == 403, result.Forbidden);

            var accessor = services.GetRequiredService<IHttpContextAccessor>();
            accessor.HttpContext = httpContext;
            try
            {
                var usuario = services.GetRequiredService<ICurrentUser>();
                if (cenario == "proprio")
                {
                    Assert.Equal(15, usuario.IdUsuario);
                    Assert.Equal("015", usuario.Codigo);
                    Assert.Equal("Usuário", usuario.Nome);
                }
                if (statusEsperado == 401) Assert.Null(usuario.IdUsuario);
            }
            finally { accessor.HttpContext = null; }
        }
    }
}
