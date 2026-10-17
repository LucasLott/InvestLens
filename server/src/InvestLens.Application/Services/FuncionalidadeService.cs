using InvestLens.Application.Interfaces.Repositories;
using InvestLens.Application.Interfaces.Services;
using InvestLens.Application.DTOs.Funcionalidade;

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

            var plano = assinatura.PlanoEfetivo;
            return await repository.PossuiPermissaoAsync(plano, chave, cancellationToken);
        }

        public async Task<IReadOnlyList<FuncionalidadeResponse>> ListarAsync(int? idUsuario, CancellationToken cancellationToken)
        {
            var plano = "Free";
            if (idUsuario is > 0)
            {
                var assinatura = await assinaturaService.ObterMinhaAsync(idUsuario.Value, cancellationToken);
                plano = assinatura.PlanoEfetivo;
            }
            var itens = await repository.ListarAsync(cancellationToken);
            return itens.Where(item => item.Pro is "S" or "N").Select(item => new FuncionalidadeResponse(
                item.Id, item.Nome, item.Nome == "CONFIGURACAO_CRITERIOS" ? "Configuração de critérios" : item.Nome.Replace('_', ' '),
                item.Pro == "S", idUsuario is > 0 ? item.Pro == "N" || plano == "Pro" : null)).ToArray();
        }
    }
}
