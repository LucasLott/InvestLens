using FluentValidation;
using InvestLens.Application.DTOs.Usuario;
using InvestLens.Application.Interfaces.Repositories.Usuario;
using InvestLens.Application.Interfaces.Security;
using InvestLens.Application.Models;
using InvestLens.Application.Services;
using InvestLens.Application.Validators;

namespace InvestLens.UnitTests
{
    public class UsuarioServiceTests
    {
        [Fact]
        public async Task ValidRequestAwaitsHashBeforePersistingAndForwardsCancellation()
        {
            var repository = new UsuarioDouble();
            var hasher = new HasherDouble();
            var service = new UsuarioService(repository, hasher, new AdicionarUsuarioRequestValidator());
            using var cancellation = new CancellationTokenSource();
            var request = Request();
            var operation = service.Adicionar(request, cancellation.Token);

            Assert.Equal(request.Senha, hasher.Password);
            Assert.Equal(0, repository.Calls);
            hasher.Completion.SetResult("generated-hash");
            await operation;

            Assert.Equal(1, repository.Calls);
            Assert.Equal("generated-hash", repository.Hash);
            Assert.Same(request, repository.Request);
            Assert.Equal(cancellation.Token, repository.Token);
        }

        [Theory]
        [InlineData("name")]
        [InlineData("long-name")]
        [InlineData("email")]
        [InlineData("long-email")]
        [InlineData("password")]
        [InlineData("confirmation")]
        [InlineData("mismatch")]
        public async Task InvalidRequestStopsBeforeHashingOrPersistence(string invalidField)
        {
            var request = Request();
            switch (invalidField)
            {
                case "name": request.Nome = " "; break;
                case "long-name": request.Nome = new string('a', 81); break;
                case "email": request.Email = "invalid"; break;
                case "long-email": request.Email = new string('a', 244) + "@example.com"; break;
                case "password": request.Senha = request.ConfirmacaoSenha = "12345"; break;
                case "confirmation": request.ConfirmacaoSenha = ""; break;
                case "mismatch": request.ConfirmacaoSenha = "different"; break;
            }
            var repository = new UsuarioDouble();
            var hasher = new HasherDouble();
            var service = new UsuarioService(repository, hasher, new AdicionarUsuarioRequestValidator());

            await Assert.ThrowsAsync<ValidationException>(() => service.Adicionar(request, CancellationToken.None));
            Assert.Null(hasher.Password);
            Assert.Equal(0, repository.Calls);
        }

        [Fact]
        public async Task CancellationDuringHashingPreventsPersistence()
        {
            var repository = new UsuarioDouble();
            var hasher = new HasherDouble();
            var service = new UsuarioService(repository, hasher, new AdicionarUsuarioRequestValidator());
            using var cancellation = new CancellationTokenSource();
            var operation = service.Adicionar(Request(), cancellation.Token);
            cancellation.Cancel();
            hasher.Completion.SetResult("generated-hash");
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
            Assert.Equal(0, repository.Calls);
        }

        [Fact]
        public void MaximumNameAndEmailLengthsAreAccepted()
        {
            var request = Request();
            request.Nome = new string('a', 80);
            request.Email = new string('a', 243) + "@example.com";
            Assert.True(new AdicionarUsuarioRequestValidator().Validate(request).IsValid);
        }

        private static AdicionarUsuarioRequest Request() => new()
        {
            Nome = "Usuário", Email = "user@example.com",
            Senha = "password", ConfirmacaoSenha = "password"
        };

        private sealed class UsuarioDouble : IUsuarioRepository
        {
            public int Calls { get; private set; }
            public string? Hash { get; private set; }
            public AdicionarUsuarioRequest? Request { get; private set; }
            public CancellationToken Token { get; private set; }
            public Task Adicionar(AdicionarUsuarioRequest request, string senhaHash, CancellationToken cancellationToken)
            {
                Calls++;
                Request = request;
                Hash = senhaHash;
                Token = cancellationToken;
                return Task.CompletedTask;
            }
            public Task<DadosLogin?> ObterDadosLoginAsync(string email, CancellationToken cancellationToken) =>
                throw new NotSupportedException();
        }

        private sealed class HasherDouble : IPasswordHasher
        {
            public string? Password { get; private set; }
            public TaskCompletionSource<string> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public Task<string> Hash(string password)
            {
                Password = password;
                return Completion.Task;
            }
            public Task<bool> Verify(string password, string passwordHash) => throw new NotSupportedException();
        }
    }
}
