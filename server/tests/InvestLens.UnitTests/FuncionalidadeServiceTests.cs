using InvestLens.Application.DTOs.Assinatura;
using InvestLens.Application.Interfaces.Repositories;
using InvestLens.Application.Interfaces.Services;
using InvestLens.Application.Services;

namespace InvestLens.UnitTests
{
    public class FuncionalidadeServiceTests
    {
        [Theory]
        [InlineData("Free", false)]
        [InlineData("Pro", true)]
        public async Task UsaPlanoEfetivoEmVezDoPlanoCadastrado(string efetivo, bool permitido)
        {
            var repository = new Repository();
            var service = new FuncionalidadeService(new Assinatura(efetivo), repository);

            Assert.Equal(permitido, await service.PossuiPermissaoAsync(42, "PRECO_MAXIMO", CancellationToken.None));
            Assert.Equal(efetivo, repository.Plano);
        }

        [Fact]
        public async Task IdentidadeInvalidaNaoConsultaPermissao()
        {
            var repository = new Repository();
            var service = new FuncionalidadeService(new Assinatura("Pro"), repository);

            Assert.False(await service.PossuiPermissaoAsync(0, "PRECO_MAXIMO", CancellationToken.None));
            Assert.Null(repository.Plano);
        }

        private sealed class Assinatura(string plano) : IAssinaturaService
        {
            public Task<MinhaAssinaturaResponse> ObterMinhaAsync(int idUsuario, CancellationToken cancellationToken) =>
                Task.FromResult(new MinhaAssinaturaResponse("Pro", plano, null, null, null, plano == "Pro"));
        }

        private sealed class Repository : IFuncionalidadeRepository
        {
            public string? Plano { get; private set; }

            public Task<bool> PossuiPermissaoAsync(string planoEfetivo, string chave, CancellationToken cancellationToken)
            {
                Plano = planoEfetivo;
                return Task.FromResult(planoEfetivo == "Pro" && chave == "PRECO_MAXIMO");
            }
        }
    }
}