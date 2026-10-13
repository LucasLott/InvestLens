using InvestLens.Application.DTOs.Assinatura;

namespace InvestLens.Application.Interfaces.Services
{
    public interface IAssinaturaService
    {
        Task<MinhaAssinaturaResponse> ObterMinhaAsync(int idUsuario, CancellationToken cancellationToken);
    }
}
