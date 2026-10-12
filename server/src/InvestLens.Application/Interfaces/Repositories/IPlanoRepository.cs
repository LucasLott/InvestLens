using InvestLens.Application.DTOs.Plano;

namespace InvestLens.Application.Interfaces.Repositories
{
    public interface IPlanoRepository
    {
        Task<IReadOnlyList<PlanoResponse>> ListarAtivosAsync(CancellationToken ct);
        
        Task<PlanoResponse?> ObterAtivoAsync(int idPlano, CancellationToken ct);
    }
}
