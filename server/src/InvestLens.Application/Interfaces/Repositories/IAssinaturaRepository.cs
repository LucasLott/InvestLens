using InvestLens.Application.Models;

namespace InvestLens.Application.Interfaces.Repositories
{
    public interface IAssinaturaRepository
    {
        Task<AssinaturaAtual?> ObterAtualAsync(int idUsuario, CancellationToken cancellationToken);
    }
}