using System.Net;
using System.Net.Http.Headers;
using InvestLens.Application.DTOs.Funcionalidade;
using InvestLens.Application.Interfaces.Security;
using InvestLens.Application.Interfaces.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InvestLens.IntegrationTests;

public class FuncionalidadesEndpointTests
{
    [Fact]
    public async Task CatalogoPublicoEOProprioCatalogoAutenticado()
    {
        await using var factory = new TestApiFactory().WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IFuncionalidadeService>();
            services.AddSingleton<IFuncionalidadeService, FakeService>();
        }));
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/funcionalidades")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/minhas-funcionalidades")).StatusCode);
        var token = factory.Services.GetRequiredService<IJwtTokenGenerator>().Generate(1, "001", "Teste", 1);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/minhas-funcionalidades")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/usuarios/1/configuracao")).StatusCode);
    }

    [Fact]
    public async Task ErroNaConsultaDePermissaoNaoLiberaConfiguracao()
    {
        await using var factory = new TestApiFactory().WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IFuncionalidadeService>();
            services.AddSingleton<IFuncionalidadeService, ServicoComFalha>();
        }));
        using var client = factory.CreateClient();
        var token = factory.Services.GetRequiredService<IJwtTokenGenerator>().Generate(1, "001", "Teste", 2);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/usuarios/1/configuracao")).StatusCode);
    }

    private sealed class ServicoComFalha : IFuncionalidadeService
    {
        public Task<bool> PossuiPermissaoAsync(int idUsuario, string chave, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Banco indisponível");
        public Task<IReadOnlyList<FuncionalidadeResponse>> ListarAsync(int? idUsuario, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Banco indisponível");
    }

    private sealed class FakeService : IFuncionalidadeService
    {
        public Task<bool> PossuiPermissaoAsync(int idUsuario, string chave, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<IReadOnlyList<FuncionalidadeResponse>> ListarAsync(int? idUsuario, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<FuncionalidadeResponse>>([new(1, "CONFIGURACAO_CRITERIOS", "Configuração de critérios", false, idUsuario.HasValue ? true : null)]);
    }
}
