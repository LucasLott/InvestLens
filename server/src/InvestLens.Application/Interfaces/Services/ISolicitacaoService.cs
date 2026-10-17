using InvestLens.Application.DTOs.Assinatura;

namespace InvestLens.Application.Interfaces.Services
{
    public interface ISolicitacaoService
    {
        Task<IReadOnlyList<SolicitacaoResponse>> ListarAsync(int idUsuario, CancellationToken cancellationToken);
        Task<SolicitacaoResponse> AlterarAsync(int idUsuario, int destino, CancellationToken cancellationToken);
        Task<SolicitacaoResponse> CancelarAsync(int idUsuario, CancellationToken cancellationToken);
    }
}