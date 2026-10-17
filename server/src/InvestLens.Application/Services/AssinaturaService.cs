using InvestLens.Application.DTOs.Assinatura;
using InvestLens.Application.Interfaces.Repositories;
using InvestLens.Application.Interfaces.Services;

namespace InvestLens.Application.Services;

public sealed class AssinaturaService(IAssinaturaRepository repository, TimeProvider timeProvider) : IAssinaturaService
{
    public async Task<MinhaAssinaturaResponse> ObterMinhaAsync(int idUsuario, CancellationToken cancellationToken)
    {
        var assinatura = await repository.ObterAtualAsync(idUsuario, cancellationToken);

        var agora = timeProvider.GetUtcNow();
        
        var premium = assinatura is not null && assinatura.PlanoAssinatura == "Pro"
            && assinatura.StatusAssinatura == "ATIVA"
            && assinatura.InicioVigencia <= agora && agora < assinatura.FimVigencia
            && await repository.PossuiPagamentoConfirmadoAsync(idUsuario, cancellationToken);

        return new MinhaAssinaturaResponse(assinatura?.PlanoCadastrado ?? "Free",
                                           premium ? "Pro" : "Free",
                                           assinatura?.StatusAssinatura,
                                           assinatura?.InicioVigencia,
                                           assinatura?.FimVigencia,
                                           premium);
    }
}
