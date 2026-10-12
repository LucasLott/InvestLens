using InvestLens.Application.DTOs.Plano;

namespace InvestLens.Application.Interfaces.Services
{
    public interface IPlanoService
    {
        Task<IReadOnlyList<PlanoResponse>> ListarAsync(CancellationToken ct);
        
        Task<PlanoResponse?> ObterAsync(int idPlano, CancellationToken ct);
    }
}
