using FluentValidation;
using InvestLens.Application.DTOs.Configuracao;
using InvestLens.Application.Exceptions;
using InvestLens.Application.Interfaces.Repositories.Configuracao;
using InvestLens.Application.Services;
using InvestLens.Application.Validators;

namespace InvestLens.UnitTests;

public class ConfiguracaoServiceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ValidacaoFalhaAntesDeConsultarOuGravar(bool alterar)
    {
        var repository = new RepositorioObservado();
        var service = new ConfiguracaoService(repository, new ConfiguracaoRequestValidator());

        await Assert.ThrowsAsync<ValidationException>(() => alterar
            ? service.AlterarAsync(10, new ConfiguracaoRequest(), CancellationToken.None)
            : service.AdicionarAsync(10, new ConfiguracaoRequest(), CancellationToken.None));

        Assert.Empty(repository.Operacoes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancelamentoImpedeGravacao(bool alterar)
    {
        var repository = new RepositorioObservado();
        var service = new ConfiguracaoService(repository, new ConfiguracaoRequestValidator());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => alterar
            ? service.AlterarAsync(10, Request(), cancellation.Token)
            : service.AdicionarAsync(10, Request(), cancellation.Token));

        Assert.Empty(repository.Operacoes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FalhaNaEscritaNaoFazUpsertNemConsultaPosterior(bool alterar)
    {
        Exception falha = alterar ? new ConfiguracaoNaoEncontradaException() : new ConfiguracaoJaExisteException();
        var repository = new RepositorioObservado { Falha = falha };
        var service = new ConfiguracaoService(repository, new ConfiguracaoRequestValidator());

        var encontrada = await Record.ExceptionAsync(() => alterar
            ? service.AlterarAsync(10, Request(), CancellationToken.None)
            : service.AdicionarAsync(10, Request(), CancellationToken.None));

        Assert.Same(falha, encontrada);
        Assert.Equal(new[] { alterar ? "alterar" : "adicionar" }, repository.Operacoes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RetornaEstadoPersistidoPreservandoUsuarioCriteriosECancelamento(bool alterar)
    {
        var persistido = new ConfiguracaoResponse { IdUsuario = 10, IdConfiguracao = 37, DYMinimo = 6.0000m };
        var repository = new RepositorioObservado { Resultado = persistido };
        var service = new ConfiguracaoService(repository, new ConfiguracaoRequestValidator());
        using var cancellation = new CancellationTokenSource();
        var request = Request();

        var resultado = alterar
            ? await service.AlterarAsync(10, request, cancellation.Token)
            : await service.AdicionarAsync(10, request, cancellation.Token);

        Assert.Same(persistido, resultado);
        Assert.Equal(new[] { alterar ? "alterar" : "adicionar", "obter" }, repository.Operacoes);
        Assert.All(repository.Usuarios, usuario => Assert.Equal(10, usuario));
        Assert.All(repository.Tokens, token => Assert.Equal(cancellation.Token, token));
        Assert.Same(request, repository.Request);
        Assert.Equal(6m, repository.Request!.DYMinimo);
        Assert.Equal(6m, repository.Request.RetornoPreco);
    }

    [Fact]
    public async Task ConsultaAusenteRetornaErroSemCriarRegistro()
    {
        var repository = new RepositorioObservado();
        var service = new ConfiguracaoService(repository, new ConfiguracaoRequestValidator());

        await Assert.ThrowsAsync<ConfiguracaoNaoEncontradaException>(() => service.ObterAsync(10, CancellationToken.None));

        Assert.Equal(new[] { "obter" }, repository.Operacoes);
    }

    [Fact]
    public async Task ConsultaExistenteNaoGrava()
    {
        var repository = new RepositorioObservado { Resultado = new ConfiguracaoResponse { IdUsuario = 10 } };
        var service = new ConfiguracaoService(repository, new ConfiguracaoRequestValidator());

        Assert.Same(repository.Resultado, await service.ObterAsync(10, CancellationToken.None));
        Assert.Equal(new[] { "obter" }, repository.Operacoes);
    }

    [Fact]
    public async Task LimitesValidosNaoRecebemTetoFinanceiroOuConversaoDePercentual()
    {
        var request = new ConfiguracaoRequest
        {
            PLMaximo = 0, DYMinimo = 9999.9999m, ROEMinimo = 0,
            DividaPatrimonioMaximo = 0, MargemLiquidaMinimo = 0, RetornoPreco = 0.0001m
        };
        var resultado = await new ConfiguracaoRequestValidator().ValidateAsync(request);
        Assert.True(resultado.IsValid);
    }

    private static ConfiguracaoRequest Request() => new()
    {
        PLMaximo = 15.1234m, DYMinimo = 6m, ROEMinimo = 15m,
        DividaPatrimonioMaximo = 1m, MargemLiquidaMinimo = 20m, RetornoPreco = 6m
    };

    private sealed class RepositorioObservado : IConfiguracaoRepository
    {
        public List<string> Operacoes { get; } = [];
        public List<int> Usuarios { get; } = [];
        public List<CancellationToken> Tokens { get; } = [];
        public Exception? Falha { get; init; }
        public ConfiguracaoResponse? Resultado { get; init; }
        public ConfiguracaoRequest? Request { get; private set; }

        public Task<ConfiguracaoResponse?> ObterAsync(int idUsuario, CancellationToken cancellationToken)
        {
            Registrar("obter", idUsuario, cancellationToken);
            return Task.FromResult(Resultado);
        }
        public Task AdicionarAsync(int idUsuario, ConfiguracaoRequest request, CancellationToken cancellationToken)
        {
            Registrar("adicionar", idUsuario, cancellationToken);
            Request = request;
            return Falha is null ? Task.CompletedTask : Task.FromException(Falha);
        }
        public Task AlterarAsync(int idUsuario, ConfiguracaoRequest request, CancellationToken cancellationToken)
        {
            Registrar("alterar", idUsuario, cancellationToken);
            Request = request;
            return Falha is null ? Task.CompletedTask : Task.FromException(Falha);
        }
        private void Registrar(string operacao, int idUsuario, CancellationToken cancellationToken)
        {
            Operacoes.Add(operacao);
            Usuarios.Add(idUsuario);
            Tokens.Add(cancellationToken);
        }
    }
}
