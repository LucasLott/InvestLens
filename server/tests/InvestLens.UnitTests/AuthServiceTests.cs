using FluentValidation;
using InvestLens.Application.DTOs.Auth;
using InvestLens.Application.DTOs.Usuario;
using InvestLens.Application.Exceptions;
using InvestLens.Application.Interfaces.Repositories.Usuario;
using InvestLens.Application.Interfaces.Security;
using InvestLens.Application.Models;
using InvestLens.Application.Services;
using InvestLens.Application.Validators;

namespace InvestLens.UnitTests
{
    public class AuthServiceTests
    {
        [Fact]
        public async Task ValidCredentialsReturnUserAndForwardEmailTokenAndPassword()
        {
            var repository = new UsuarioDouble(CreateUser());
            var hasher = new HasherDouble(true);
            using var cancellation = new CancellationTokenSource();
            var generator = new JwtDouble();
            var service = new AuthService(repository, hasher, new LoginRequestValidator(), generator);

            var response = await service.LoginAsync(new LoginRequest { Email = "user@example.com", Senha = "x" }, cancellation.Token);

            Assert.Equal(7, response.IdUsuario);
            Assert.Equal("007", response.Codigo);
            Assert.Equal("Usuário", response.Nome);
            Assert.Equal("user@example.com", response.Email);
            Assert.Equal("user@example.com", repository.Email);
            Assert.Equal(cancellation.Token, repository.Token);
            Assert.Equal(("x", "stored-hash"), hasher.Verified);
            Assert.Equal((7, "007", "Usuário"), generator.User);
            Assert.Equal(1, generator.Calls);
            Assert.Same(generator.Result, response.Token);
            Assert.Equal("test-access-token", response.Token.AccessToken);
            Assert.Equal(generator.Result.Expiration, response.Token.Expiration);
        }

        [Theory]
        [InlineData(true, true, false, 1)]
        [InlineData(false, true, true, 0)]
        [InlineData(true, false, true, 0)]
        public async Task InvalidCredentialsHaveSameException(bool found, bool active, bool verified, int verifyCalls)
        {
            var repository = new UsuarioDouble(found ? CreateUser(active) : null);
            var hasher = new HasherDouble(verified);
            var generator = new JwtDouble();
            var service = new AuthService(repository, hasher, new LoginRequestValidator(), generator);

            var exception = await Assert.ThrowsAsync<CredenciaisInvalidasException>(() =>
                service.LoginAsync(new LoginRequest { Email = "user@example.com", Senha = "x" }, CancellationToken.None));

            Assert.Equal("Credenciais inválidas.", exception.Message);
            Assert.Equal(verifyCalls, hasher.Calls);
            Assert.Equal(0, generator.Calls);
        }

        [Theory]
        [InlineData(null, "x")]
        [InlineData("", "x")]
        [InlineData("invalid-email", "x")]
        [InlineData("user@example.com", null)]
        [InlineData("user@example.com", "")]
        [InlineData("user@example.com", "   ")]
        public async Task InvalidRequestDoesNotAccessRepository(string? email, string? password)
        {
            var repository = new UsuarioDouble(CreateUser());
            var hasher = new HasherDouble(true);
            var generator = new JwtDouble();
            var service = new AuthService(repository, hasher, new LoginRequestValidator(), generator);
            await Assert.ThrowsAsync<ValidationException>(() => service.LoginAsync(
                new LoginRequest { Email = email!, Senha = password! }, CancellationToken.None));
            Assert.Null(repository.Email);
            Assert.Equal(0, hasher.Calls);
        }

        [Theory]
        [InlineData(255, true)]
        [InlineData(256, false)]
        public void EmailLengthMatchesDatabaseAndPasswordHasNoComplexityRule(int length, bool valid)
        {
            var result = new LoginRequestValidator().Validate(new LoginRequest
            {
                Email = new string('a', length - "@example.com".Length) + "@example.com",
                Senha = "x"
            });
            Assert.Equal(valid, result.IsValid);
        }

        [Fact]
        public async Task CancellationFromRepositoryIsNotConvertedToInvalidCredentials()
        {
            using var cancellation = new CancellationTokenSource();
            var repository = new UsuarioDouble(CreateUser()) { Cancel = cancellation.Cancel };
            var hasher = new HasherDouble(true);
            var generator = new JwtDouble();
            var service = new AuthService(repository, hasher, new LoginRequestValidator(), generator);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.LoginAsync(
                new LoginRequest { Email = "user@example.com", Senha = "x" }, cancellation.Token));
            Assert.Equal(0, hasher.Calls);
        }

        private static DadosLogin CreateUser(bool active = true) => new()
        {
            IdUsuario = 7, Codigo = "007", Nome = "Usuário", Email = "user@example.com",
            SenhaHash = "stored-hash", Ativo = active
        };

        private sealed class JwtDouble : IJwtTokenGenerator
        {
            public int Calls { get; private set; }
            public (int Id, string Codigo, string Nome)? User { get; private set; }
            public Token Result { get; } = new()
            {
                AccessToken = "test-access-token",
                Expiration = new DateTime(2030, 1, 2, 3, 4, 5, DateTimeKind.Utc)
            };
            public Token Generate(int idUsuario, string codigo, string nome)
            {
                Calls++;
                User = (idUsuario, codigo, nome);
                return Result;
            }
        }

        private sealed class UsuarioDouble(DadosLogin? user) : IUsuarioRepository
        {
            public string? Email { get; private set; }
            public CancellationToken Token { get; private set; }
            public Action? Cancel { get; init; }
            public Task<DadosLogin?> ObterDadosLoginAsync(string email, CancellationToken cancellationToken)
            {
                Email = email;
                Token = cancellationToken;
                Cancel?.Invoke();
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(user);
            }
            public Task Adicionar(AdicionarUsuarioRequest request, string senhaHash, CancellationToken cancellationToken) =>
                throw new NotSupportedException();
        }

        private sealed class HasherDouble(bool verified) : IPasswordHasher
        {
            public int Calls { get; private set; }
            public (string Password, string Hash)? Verified { get; private set; }
            public Task<string> Hash(string password) => throw new NotSupportedException();
            public Task<bool> Verify(string password, string passwordHash)
            {
                Calls++;
                Verified = (password, passwordHash);
                return Task.FromResult(verified);
            }
        }
    }
}
