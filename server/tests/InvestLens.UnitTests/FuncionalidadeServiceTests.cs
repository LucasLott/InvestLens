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

        [Fact]
        public async Task ListaSomenteValoresValidosESinalizaAcessoFree()
        {
            var repository = new Repository { Itens = [(1, "BASICO", "N"), (2, "AVANCADO", "S"), (3, "INVALIDO", "X")] };
            var service = new FuncionalidadeService(new Assinatura("Free"), repository);
            var resultado = await service.ListarAsync(42, CancellationToken.None);
            Assert.Equal(2, resultado.Count);
            Assert.True(resultado.Single(x => x.Nome == "BASICO").Disponivel);
            Assert.False(resultado.Single(x => x.Nome == "AVANCADO").Disponivel);
        }

        private sealed class Assinatura(string plano) : IAssinaturaService
        {
            public Task<MinhaAssinaturaResponse> ObterMinhaAsync(int idUsuario, CancellationToken cancellationToken) =>
                Task.FromResult(new MinhaAssinaturaResponse("Pro", plano, null, null, null, plano == "Pro"));
        }

        private sealed class Repository : IFuncionalidadeRepository
        {
            public string? Plano { get; private set; }
            public IReadOnlyList<(int Id, string Nome, string Pro)> Itens { get; init; } = [];
            public Task<IReadOnlyList<(int Id, string Nome, string Pro)>> ListarAsync(CancellationToken cancellationToken) => Task.FromResult(Itens);

            public Task<bool> PossuiPermissaoAsync(string planoEfetivo, string chave, CancellationToken cancellationToken)
            {
                Plano = planoEfetivo;
                return Task.FromResult(planoEfetivo == "Pro" && chave == "PRECO_MAXIMO");
            }
        }
    }
}
