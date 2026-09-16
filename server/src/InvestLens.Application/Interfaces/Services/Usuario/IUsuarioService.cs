using InvestLens.Application.DTOs.Usuario;

namespace InvestLens.Application.Interfaces.Services.Usuario
{
    public interface IUsuarioService
    {
        Task Adicionar(AdicionarUsuarioRequest request, CancellationToken cancellationToken);
    }
}