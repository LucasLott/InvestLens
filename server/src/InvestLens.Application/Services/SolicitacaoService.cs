using InvestLens.Application.DTOs.Assinatura;
using InvestLens.Application.Interfaces.Repositories;
using InvestLens.Application.Interfaces.Services;
using InvestLens.Domain.Exceptions;

namespace InvestLens.Application.Services
{
    public sealed class SolicitacaoService(ISolicitacaoRepository repository) : ISolicitacaoService
    {
        public Task<IReadOnlyList<SolicitacaoResponse>> ListarAsync(int idUsuario, CancellationToken cancellationToken) =>
            repository.ListarAsync(idUsuario, cancellationToken);

        public Task<SolicitacaoResponse> AlterarAsync(int idUsuario, int destino, CancellationToken cancellationToken)
        {
            if (destino <= 0) throw new BusinessException();
            return repository.RegistrarAsync(idUsuario, "ALT", destino, cancellationToken);
        }

        public Task<SolicitacaoResponse> CancelarAsync(int idUsuario, CancellationToken cancellationToken) =>
            repository.RegistrarAsync(idUsuario, "CAN", null, cancellationToken);
    }
}