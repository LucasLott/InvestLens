using InvestLens.Application.DTOs.Usuario;

namespace InvestLens.Application.Interfaces.Repositories.Usuario
{
    public interface IUsuarioRepository
    {
        Task Adicionar(AdicionarUsuarioRequest request, CancellationToken cancellationToken);
    }
}