using InvestLens.Application.DTOs.Configuracao;

namespace InvestLens.Application.Interfaces.Services.Configuracao
{
    public interface IConfiguracaoService
    {
        Task<ConfiguracaoResponse> ObterAsync(int idUsuario, CancellationToken cancellationToken);
        Task<ConfiguracaoResponse> AdicionarAsync(int idUsuario, ConfiguracaoRequest request, CancellationToken cancellationToken);
        Task<ConfiguracaoResponse> AlterarAsync(int idUsuario, ConfiguracaoRequest request, CancellationToken cancellationToken);
    }
}
