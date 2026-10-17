using InvestLens.Application.Interfaces.Repositories;
using InvestLens.Application.Models;
using InvestLens.Application.Services;

namespace InvestLens.UnitTests;

public class AssinaturaPlanoEfetivoTests
{
    [Theory]
    [InlineData(true, true, "Pro")]
    [InlineData(false, true, "Free")]
    [InlineData(true, false, "Free")]
    public async Task PlanoEfetivoExigeVigenciaEPagamento(bool pagamento, bool vigente, string esperado)
    {
        var agora = DateTimeOffset.UtcNow;
        var repo = new Repositorio(pagamento, new AssinaturaAtual
        {
            PlanoCadastrado = "Pro", PlanoAssinatura = "Pro", StatusAssinatura = "ATIVA",
            InicioVigencia = agora.AddDays(-10), FimVigencia = agora.AddDays(vigente ? 10 : -1)
        });
        var resultado = await new AssinaturaService(repo, TimeProvider.System).ObterMinhaAsync(1, CancellationToken.None);
        Assert.Equal(esperado, resultado.PlanoEfetivo);
        Assert.Equal(esperado == "Pro", resultado.AcessoPremiumAtivo);
    }

    private sealed class Repositorio(bool pagamento, AssinaturaAtual assinatura) : IAssinaturaRepository
    {
        public Task<AssinaturaAtual?> ObterAtualAsync(int idUsuario, CancellationToken cancellationToken) => Task.FromResult<AssinaturaAtual?>(assinatura);
        public Task<bool> PossuiPagamentoConfirmadoAsync(int idUsuario, CancellationToken cancellationToken) => Task.FromResult(pagamento);
    }
}
