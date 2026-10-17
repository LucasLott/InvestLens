using InvestLens.Application.DTOs.Assinatura;
using InvestLens.Application.Interfaces.Repositories;
using InvestLens.Application.Services;
using InvestLens.Domain.Exceptions;

namespace InvestLens.UnitTests;

public class SolicitacaoServiceTests
{
    [Fact]
    public async Task AlteracaoEncaminhaIdentidadeEPlanoSemModificarAssinatura()
    {
        var repo = new FakeRepository();
        var resultado = await new SolicitacaoService(repo).AlterarAsync(12, 5, CancellationToken.None);
        Assert.Equal((12, "ALT", (int?)5), repo.Chamada);
        Assert.Equal("PEN", resultado.Status);
    }

    [Fact]
    public async Task CancelamentoNaoInformaPlanoDestino()
    {
        var repo = new FakeRepository();
        await new SolicitacaoService(repo).CancelarAsync(12, CancellationToken.None);
        Assert.Equal((12, "CAN", (int?)null), repo.Chamada);
    }

    [Fact]
    public async Task PlanoInvalidoNaoChegaAoRepositorio()
    {
        var repo = new FakeRepository();
        await Assert.ThrowsAsync<BusinessException>(() => new SolicitacaoService(repo).AlterarAsync(12, 0, CancellationToken.None));
        Assert.Null(repo.Chamada);
    }

    private sealed class FakeRepository : ISolicitacaoRepository
    {
        public (int, string, int?)? Chamada { get; private set; }
        public Task<IReadOnlyList<SolicitacaoResponse>> ListarAsync(int idUsuario, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SolicitacaoResponse>>([]);
        public Task<SolicitacaoResponse> RegistrarAsync(int idUsuario, string tipo, int? destino, CancellationToken cancellationToken)
        {
            Chamada = (idUsuario, tipo, destino);
            return Task.FromResult(new SolicitacaoResponse(1, tipo, "PEN", "Free", "Pro", DateTime.UtcNow));
        }
    }
}
