using InvestLens.Application.DTOs.Usuario;
using InvestLens.Application.Models;

namespace InvestLens.Application.Interfaces.Repositories.Usuario
{
    public interface IUsuarioRepository
    {
        Task Adicionar(AdicionarUsuarioRequest request, string senhaHash, CancellationToken cancellationToken);

        Task<DadosLogin?> ObterDadosLoginAsync(string email, CancellationToken cancellationToken);
    }
}
