using InvestLens.Application.DTOs.Usuario;
using InvestLens.Application.Interfaces.Repositories.Usuario;
using InvestLens.Application.Interfaces.Security;
using InvestLens.Application.Interfaces.Services.Usuario;

namespace InvestLens.Application.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IPasswordHasher _passwordHasher;

        public UsuarioService(IUsuarioRepository usuarioRepository,
                              IPasswordHasher passwordHasher)
        {
            _usuarioRepository = usuarioRepository;
            _passwordHasher = passwordHasher;
        }

        public Task Adicionar(AdicionarUsuarioRequest request,
                              CancellationToken cancellationToken)
        {
            // Implementação do método Adicionar
            throw new NotImplementedException();
        }
    }
}