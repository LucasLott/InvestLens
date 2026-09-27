using FluentValidation;
using InvestLens.Application.DTOs.Configuracao;
using InvestLens.Application.Exceptions;
using InvestLens.Application.Interfaces.Repositories.Configuracao;
using InvestLens.Application.Interfaces.Services.Configuracao;

namespace InvestLens.Application.Services
{
    public sealed class ConfiguracaoService(IConfiguracaoRepository configuracaoRepository,
                                            IValidator<ConfiguracaoRequest> validator) : IConfiguracaoService
    {
        public async Task<ConfiguracaoResponse> ObterAsync(int idUsuario, CancellationToken cancellationToken) =>
            await configuracaoRepository.ObterAsync(idUsuario, cancellationToken) ?? throw new ConfiguracaoNaoEncontradaException();

        public async Task<ConfiguracaoResponse> AdicionarAsync(int idUsuario, ConfiguracaoRequest request, CancellationToken cancellationToken)
        {
            await validator.ValidateAndThrowAsync(request, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            await configuracaoRepository.AdicionarAsync(idUsuario, request, cancellationToken);
            return await ObterAsync(idUsuario, cancellationToken);
        }

        public async Task<ConfiguracaoResponse> AlterarAsync(int idUsuario, ConfiguracaoRequest request, CancellationToken cancellationToken)
        {
            await validator.ValidateAndThrowAsync(request, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            await configuracaoRepository.AlterarAsync(idUsuario, request, cancellationToken);
            return await ObterAsync(idUsuario, cancellationToken);
        }
    }
}
