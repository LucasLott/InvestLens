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
                                    IValidator<LoginRequest> validator,
                                    IJwtTokenGenerator jwtTokenGenerator) : IAuthService
    {
        private readonly IUsuarioRepository _usuarioRepository = usuarioRepository;
        private readonly IPasswordHasher _passwordHasher = passwordHasher;
        private readonly IValidator<LoginRequest> _validator = validator;
        private readonly IJwtTokenGenerator _jwtTokenGenerator = jwtTokenGenerator;

        public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
        {
            await _validator.ValidateAndThrowAsync(request, cancellationToken);

            var usuario = await _usuarioRepository.ObterDadosLoginAsync(request.Email, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (usuario is null || !usuario.Ativo || !await _passwordHasher.Verify(request.Senha, usuario.SenhaHash))
                throw new CredenciaisInvalidasException();

            cancellationToken.ThrowIfCancellationRequested();
            var token = _jwtTokenGenerator.Generate(usuario.IdUsuario, usuario.Codigo, usuario.Nome);

            return new LoginResponse
            {
                IdUsuario = usuario.IdUsuario,
                Codigo = usuario.Codigo,
                Nome = usuario.Nome,
                Email = usuario.Email,
                Token = token
            };
        }
    }
}
