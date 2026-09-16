using FluentValidation;
using InvestLens.Application.DTOs.Auth;
using InvestLens.Application.Exceptions;
using InvestLens.Application.Interfaces.Repositories.Usuario;
using InvestLens.Application.Interfaces.Security;
using InvestLens.Application.Interfaces.Services.Auth;

namespace InvestLens.Application.Services
{
    public sealed class AuthService(IUsuarioRepository usuarioRepository,
                                    IPasswordHasher passwordHasher,
                                    IValidator<LoginRequest> validator) : IAuthService
    {
        public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
        {
            await validator.ValidateAndThrowAsync(request, cancellationToken);

            var usuario = await usuarioRepository.ObterDadosLoginAsync(request.Email, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (usuario is null || !usuario.Ativo || !await passwordHasher.Verify(request.Senha, usuario.SenhaHash))
                throw new CredenciaisInvalidasException();

            return new LoginResponse
            {
                IdUsuario = usuario.IdUsuario,
                Codigo = usuario.Codigo,
                Nome = usuario.Nome,
                Email = usuario.Email
            };
        }
    }
}
