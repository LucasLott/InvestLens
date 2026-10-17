using InvestLens.Application.DTOs.Assinatura;

namespace InvestLens.Application.Interfaces.Repositories
{
    public interface ISolicitacaoRepository
    {
        Task<IReadOnlyList<SolicitacaoResponse>> ListarAsync(int idUsuario, CancellationToken cancellationToken);
        Task<SolicitacaoResponse> RegistrarAsync(int idUsuario, string tipo, int? destino, CancellationToken cancellationToken);
    }
}