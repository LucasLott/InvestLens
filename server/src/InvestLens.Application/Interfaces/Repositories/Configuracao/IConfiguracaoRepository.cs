using InvestLens.Application.DTOs.Configuracao;

namespace InvestLens.Application.Interfaces.Repositories.Configuracao
{
    public interface IConfiguracaoRepository
    {
        Task<ConfiguracaoResponse?> ObterAsync(int idUsuario, CancellationToken cancellationToken);
        Task AdicionarAsync(int idUsuario, ConfiguracaoRequest request, CancellationToken cancellationToken);
        Task AlterarAsync(int idUsuario, ConfiguracaoRequest request, CancellationToken cancellationToken);
    }
}
