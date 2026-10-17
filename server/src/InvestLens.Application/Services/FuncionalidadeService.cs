using InvestLens.Application.Interfaces.Repositories;
using InvestLens.Application.Interfaces.Services;

namespace InvestLens.Application.Services
{
    public sealed class FuncionalidadeService(IAssinaturaService assinaturaService,
                                              IFuncionalidadeRepository repository) : IFuncionalidadeService
    {
        public async Task<bool> PossuiPermissaoAsync(int idUsuario,
                                                     string chave,
                                                     CancellationToken cancellationToken)
        {
            if (idUsuario <= 0 || string.IsNullOrWhiteSpace(chave))
                return false;

            var assinatura = await assinaturaService.ObterMinhaAsync(idUsuario, cancellationToken);

            return await repository.PossuiPermissaoAsync(assinatura.PlanoEfetivo, chave, cancellationToken);
        }
    }
}