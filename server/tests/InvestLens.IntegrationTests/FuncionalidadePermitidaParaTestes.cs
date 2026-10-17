using InvestLens.Application.DTOs.Funcionalidade;
using InvestLens.Application.Interfaces.Services;

namespace InvestLens.IntegrationTests;

// Os testes de configuração isolam a autorização da funcionalidade para testar SameUser e persistência.
internal sealed class FuncionalidadePermitidaParaTestes : IFuncionalidadeService
{
    public Task<bool> PossuiPermissaoAsync(int idUsuario, string chave, CancellationToken cancellationToken) =>
        Task.FromResult(chave == "CONFIGURACAO_CRITERIOS");

    public Task<IReadOnlyList<FuncionalidadeResponse>> ListarAsync(int? idUsuario, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<FuncionalidadeResponse>>([]);
}
