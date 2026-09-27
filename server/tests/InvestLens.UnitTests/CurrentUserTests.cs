using System.Security.Claims;
using InvestLens.Api.Authentication;
using InvestLens.Application.Authentication;
using Microsoft.AspNetCore.Http;

namespace InvestLens.UnitTests
{
    public class CurrentUserTests
    {
        [Fact]
        public void RetornaClaimsDoUsuarioAutenticado()
        {
            var usuario = CriarUsuario("15");
            Assert.Equal(15, usuario.IdUsuario);
            Assert.Equal("015", usuario.Codigo);
            Assert.Equal("Usuário", usuario.Nome);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("ABC")]
        [InlineData("0")]
        [InlineData("-1")]
        [InlineData("2147483648")]
        public void IdAusenteOuInvalidoRetornaNull(string? idUsuario)
        {
            Assert.Null(CriarUsuario(idUsuario).IdUsuario);
        }

        [Fact]
        public void SemContextoOuSemAutenticacaoNaoExpoeClaims()
        {
            var accessor = new HttpContextAccessor();
            var usuario = new CurrentUser(accessor);
            Assert.Null(usuario.IdUsuario);
            Assert.Null(usuario.Codigo);
            Assert.Null(usuario.Nome);

            accessor.HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([
                    new Claim(JwtClaimNames.IdUsuario, "15"),
                    new Claim(JwtClaimNames.Codigo, "015"),
                    new Claim(JwtClaimNames.Nome, "Usuário")]))
            };
            Assert.Null(usuario.IdUsuario);
            Assert.Null(usuario.Codigo);
            Assert.Null(usuario.Nome);
        }

        [Fact]
        public void NaoUsaNameIdentifierNemClaimsDeIdentidadeAnonima()
        {
            var accessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal([
                        new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "15")], "Bearer"),
                        new ClaimsIdentity([new Claim(JwtClaimNames.IdUsuario, "15")])])
                }
            };
            Assert.Null(new CurrentUser(accessor).IdUsuario);
        }

        [Fact]
        public void ClaimsDuplicadasNaoSaoAceitas()
        {
            var accessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([
                        new Claim(JwtClaimNames.IdUsuario, "15"), new Claim(JwtClaimNames.IdUsuario, "27"),
                        new Claim(JwtClaimNames.Codigo, "015"), new Claim(JwtClaimNames.Codigo, "027"),
                        new Claim(JwtClaimNames.Nome, "A"), new Claim(JwtClaimNames.Nome, "B")], "Bearer"))
                }
            };
            var usuario = new CurrentUser(accessor);
            Assert.Null(usuario.IdUsuario);
            Assert.Null(usuario.Codigo);
            Assert.Null(usuario.Nome);
        }

        private static CurrentUser CriarUsuario(string? idUsuario)
        {
            var claims = new List<Claim> { new(JwtClaimNames.Codigo, "015"), new(JwtClaimNames.Nome, "Usuário") };
            if (idUsuario is not null) claims.Add(new(JwtClaimNames.IdUsuario, idUsuario));
            return new CurrentUser(new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer")) }
            });
        }
    }
}
