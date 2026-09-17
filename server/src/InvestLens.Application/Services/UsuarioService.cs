using FluentValidation;
using InvestLens.Application.DTOs.Usuario;
using InvestLens.Application.Interfaces.Repositories.Usuario;
using InvestLens.Application.Interfaces.Security;
using InvestLens.Application.Interfaces.Services.Usuario;

namespace InvestLens.Application.Services
{
    public class UsuarioService(IUsuarioRepository usuarioRepository,
                                IPasswordHasher passwordHasher,
                                IValidator<AdicionarUsuarioRequest> validator) : IUsuarioService
    {
        private readonly IUsuarioRepository _usuarioRepository = usuarioRepository;
        private readonly IPasswordHasher _passwordHasher = passwordHasher;
        private readonly IValidator<AdicionarUsuarioRequest> _validator = validator;

        public async Task Adicionar(AdicionarUsuarioRequest request,
                                    CancellationToken cancellationToken)
        {
            await _validator.ValidateAndThrowAsync(request, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var senhaHash = await _passwordHasher.Hash(request.Senha);
            cancellationToken.ThrowIfCancellationRequested();

            await _usuarioRepository.Adicionar(request, senhaHash, cancellationToken);
        }
    }
}
