using InvestLens.Application.DTOs.Plano;
using InvestLens.Application.Interfaces.Repositories;
using InvestLens.Application.Interfaces.Services;

namespace InvestLens.Application.Services
{
    public sealed class PlanoService(IPlanoRepository repository) : IPlanoService
    {
        public Task<IReadOnlyList<PlanoResponse>> ListarAsync(CancellationToken ct)
        {
            return repository.ListarAtivosAsync(ct);
        }

        public Task<PlanoResponse?> ObterAsync(int idPlano, CancellationToken ct)
        {
            return repository.ObterAtivoAsync(idPlano, ct);
        }
    }
}
