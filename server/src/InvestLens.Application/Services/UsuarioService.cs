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

        public async Task Adicionar(AdicionarUsuarioRequest request,
                                    CancellationToken cancellationToken)
        {
            if (!SenhasSaoIguais(request.Senha, request.ConfirmacaoSenha))
                throw new InvalidOperationException("As senhas não coincidem.");

            var senhaHash = await _passwordHasher.Hash(request.Senha);

            await _usuarioRepository.Adicionar(request, senhaHash, cancellationToken);
        }

        private bool SenhasSaoIguais(string senha, string confirmacaoSenha)
        {
            return senha == confirmacaoSenha;
        }
    }
}