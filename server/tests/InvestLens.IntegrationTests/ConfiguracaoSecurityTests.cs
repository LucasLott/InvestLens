using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using InvestLens.Api;
using InvestLens.Application.DTOs.Configuracao;
using InvestLens.Application.Exceptions;
using InvestLens.Application.Interfaces.Repositories.Configuracao;
using InvestLens.Application.Interfaces.Services.Configuracao;
using InvestLens.Application.Services;
using InvestLens.Application.Validators;
using InvestLens.Infrastructure.Configuration;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace InvestLens.IntegrationTests;

// Pipeline HTTP/JWT real; somente a persistência é substituída. Não comprova locks/UNIQUE do SQL.
public class ConfiguracaoSecurityTests
{
    public static IEnumerable<object[]> AutenticacaoCasos() =>
        from metodo in new[] { "GET", "POST", "PUT" }
        from token in new[] { "ausente", "malformado", "expirado", "assinatura-invalida" }
        select new object[] { metodo, token };

    [Theory]
    [MemberData(nameof(AutenticacaoCasos))]
    public async Task JwtRejeitadoNaoExecutaServiceNemRepository(string metodo, string token)
    {
        var repository = new RepositorioObservado();
        var service = new ServicoObservado(repository);
        await using var factory = CriarFactory(repository, service);
        using var client = factory.CreateClient();
        using var request = CriarRequest(metodo, 10);
        if (token != "ausente")
            request.Headers.Authorization = new("Bearer", token == "malformado" ? "token-invalido-marcador" : Token(factory, token));

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, service.Chamadas);
        Assert.Equal(0, repository.Chamadas);
        await ProblemaSemDados(response, 401);
        Assert.DoesNotContain("error_description", response.Headers.WwwAuthenticate.ToString());
    }

    public static IEnumerable<object[]> AutorizacaoCasos() =>
        from metodo in new[] { "GET", "POST", "PUT" }
        from existente in new[] { true, false }
        from claim in new[] { "outro", "ausente", "duplicada", "invalida" }
        select new object[] { metodo, existente, claim };

    [Theory]
    [MemberData(nameof(AutorizacaoCasos))]
    public async Task NegativaNaoConsultaNemRevelaExistenciaDoRecurso(string metodo, bool existente, string claim)
    {
        var repository = new RepositorioObservado();
        if (existente) repository.Preparar(11);
        var service = new ServicoObservado(repository);
        await using var factory = CriarFactory(repository, service);
        using var client = factory.CreateClient();
        using var request = CriarRequest(metodo, 11);
        request.Headers.Authorization = new("Bearer", Token(factory, claim));

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, service.Chamadas);
        Assert.Equal(0, repository.Chamadas);
        await ProblemaSemDados(response, 403);
    }

    public static IEnumerable<object[]> ManipulacaoCasos() =>
        from metodo in new[] { "GET", "POST", "PUT" }
        from origem in new[] { "body", "query", "header" }
        from propria in new[] { true, false }
        select new object[] { metodo, origem, propria };

    [Theory]
    [MemberData(nameof(ManipulacaoCasos))]
    public async Task UsuarioAlvoVemDaRotaMesmoComIdentificadoresExternos(string metodo, string origem, bool propria)
    {
        var repository = new RepositorioObservado();
        if (metodo != "POST") repository.Preparar(10);
        repository.Preparar(11);
        var outroAntes = JsonSerializer.Serialize(repository.Registros[11]);
        var service = new ServicoObservado(repository);
        await using var factory = CriarFactory(repository, service);
        using var client = factory.CreateClient();
        var idExterno = propria ? 11 : 10;
        var body = Body();
        if (origem == "body")
        {
            body["idUsuario"] = idExterno;
            body["ID_Usuario"] = idExterno;
            body["idConfiguracao"] = 987654;
            body["ID_Configuracao"] = 987654;
            body["dataInclusao"] = "1900-01-01T00:00:00";
            body["DH_Inclusao"] = "1900-01-01T00:00:00";
            body["dataAlteracao"] = "1900-01-01T00:00:00";
            body["DH_Alteracao"] = "1900-01-01T00:00:00";
        }
        var uri = $"/api/usuarios/{(propria ? 10 : 11)}/configuracao";
        if (origem == "query") uri += $"?idUsuario={idExterno}&ID_Usuario={idExterno}&idConfiguracao=987654";
        using var request = new HttpRequestMessage(new HttpMethod(metodo), uri) { Content = JsonContent.Create(body) };
        request.Headers.Authorization = new("Bearer", Token(factory));
        if (origem == "header")
        {
            request.Headers.Add("X-User-Id", idExterno.ToString());
            request.Headers.Add("IdUsuario", idExterno.ToString());
            request.Headers.Add("ID_Usuario", idExterno.ToString());
        }

        using var response = await client.SendAsync(request);

        Assert.Equal(propria ? metodo == "POST" ? 201 : 200 : 403, (int)response.StatusCode);
        Assert.Equal(outroAntes, JsonSerializer.Serialize(repository.Registros[11]));
        if (!propria)
        {
            Assert.Equal(0, service.Chamadas);
            Assert.Equal(0, repository.Chamadas);
            await ProblemaSemDados(response, 403);
            return;
        }
        var resultado = await response.Content.ReadFromJsonAsync<ConfiguracaoResponse>();
        Assert.NotNull(resultado);
        Assert.Equal(10, resultado.IdUsuario);
        Assert.Equal(110, resultado.IdConfiguracao);
        Assert.Equal(RepositorioData(), resultado.DataInclusao);
        Assert.NotEqual(new DateTime(1900, 1, 1), resultado.DataAlteracao);
        Assert.All(repository.UsuariosConsultados, id => Assert.Equal(10, id));
    }

    public static IEnumerable<object[]> ValidacaoCasos()
    {
        foreach (var metodo in new[] { "POST", "PUT" })
        {
            foreach (var propriedade in Body().Keys)
                foreach (var caso in new[] { "negativo", "nulo", "ausente", "precisao", "escala", "texto" })
                    yield return [metodo, propriedade, caso];
            yield return [metodo, "retornoPreco", "zero"];
        }
    }

    [Theory]
    [MemberData(nameof(ValidacaoCasos))]
    public async Task CriteriosInvalidosNaoChegamAoRepository(string metodo, string propriedade, string caso)
    {
        var repository = new RepositorioObservado();
        if (metodo == "PUT") repository.Preparar(10);
        var antes = JsonSerializer.Serialize(repository.Registros);
        await using var factory = CriarFactory(repository, new(repository));
        using var client = factory.CreateClient();
        var body = Body();
        if (caso == "ausente") body.Remove(propriedade);
        else body[propriedade] = caso switch
        {
            "negativo" => -0.0001m, "zero" => 0m, "nulo" => null,
            "precisao" => 10000m, "escala" => 1.00001m,
            _ => "1; DROP TABLE dbo.ILCFG001;--"
        };
        using var request = CriarRequest(metodo, 10, body);
        request.Headers.Authorization = new("Bearer", Token(factory));

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, repository.Chamadas);
        Assert.Equal(antes, JsonSerializer.Serialize(repository.Registros));
        await ProblemaSemDados(response, 400);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    public async Task PreservaDecimaisEPercentuaisNoContratoDePersistencia(string metodo)
    {
        var repository = new RepositorioObservado();
        if (metodo == "PUT") repository.Preparar(10);
        await using var factory = CriarFactory(repository, new(repository));
        using var client = factory.CreateClient();
        var body = Body();
        body["plMaximo"] = 15.1234m;
        body["roeMinimo"] = 15.5000m;
        body["dividaPatrimonioMaximo"] = 1.0001m;
        body["margemLiquidaMinimo"] = 20.9999m;
        using var request = CriarRequest(metodo, 10, body);
        request.Headers.Authorization = new("Bearer", Token(factory));

        using var response = await client.SendAsync(request);

        Assert.Equal(metodo == "POST" ? HttpStatusCode.Created : HttpStatusCode.OK, response.StatusCode);
        var resultado = await response.Content.ReadFromJsonAsync<ConfiguracaoResponse>();
        Assert.NotNull(resultado);
        Assert.Equal(15.1234m, resultado.PLMaximo);
        Assert.Equal(6.0000m, resultado.DYMinimo);
        Assert.Equal(15.5000m, resultado.ROEMinimo);
        Assert.Equal(1.0001m, resultado.DividaPatrimonioMaximo);
        Assert.Equal(20.9999m, resultado.MargemLiquidaMinimo);
        Assert.Equal(6.0000m, repository.UltimoRequest!.RetornoPreco);
        Assert.Equal(6.0000m, repository.UltimoRequest.DYMinimo);
    }

    [Fact]
    public async Task GetNaoGravaPostNaoSobrescreveEPutNaoCria()
    {
        var repository = new RepositorioObservado();
        await using var factory = CriarFactory(repository, new(repository));
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", Token(factory));

        using var getAusente = await client.GetAsync("/api/usuarios/10/configuracao");
        using var putAusente = await client.PutAsJsonAsync("/api/usuarios/10/configuracao", Body());
        Assert.Equal(HttpStatusCode.NotFound, getAusente.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, putAusente.StatusCode);
        Assert.Empty(repository.Registros);
        Assert.Equal(0, repository.Inclusoes);

        using var post = await client.PostAsJsonAsync("/api/usuarios/10/configuracao", Body());
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);
        var antes = JsonSerializer.Serialize(repository.Registros);
        var body = Body();
        body["plMaximo"] = 99m;
        using var duplicado = await client.PostAsJsonAsync("/api/usuarios/10/configuracao", body);
        Assert.Equal(HttpStatusCode.Conflict, duplicado.StatusCode);
        Assert.Equal(antes, JsonSerializer.Serialize(repository.Registros));
        var gravacoes = repository.Inclusoes + repository.Alteracoes;
        using var get = await client.GetAsync("/api/usuarios/10/configuracao");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Equal(antes, JsonSerializer.Serialize(repository.Registros));
        Assert.Equal(gravacoes, repository.Inclusoes + repository.Alteracoes);
        Assert.Single(repository.Registros);
    }

    private static WebApplicationFactory<Program> CriarFactory(RepositorioObservado repository, ServicoObservado service) =>
        new TestApiFactory().WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IConfiguracaoRepository>();
            services.RemoveAll<IConfiguracaoService>();
            services.AddSingleton<IConfiguracaoRepository>(repository);
            services.AddSingleton<IConfiguracaoService>(service);
        }));

    private static string Token(WebApplicationFactory<Program> factory, string caso = "valido")
    {
        var settings = factory.Services.GetRequiredService<IOptions<JwtSettings>>().Value;
        var claims = new List<Claim>();
        if (caso != "ausente") claims.Add(new("IdUsuario", caso == "invalida" ? "abc" : "10"));
        if (caso == "duplicada") claims.Add(new("IdUsuario", "11"));
        var key = caso == "assinatura-invalida" ? new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32)) : settings.CreateSigningKey();
        var jwt = new JwtSecurityToken(settings.Issuer, settings.Audience, claims,
            DateTime.UtcNow.AddMinutes(-10), caso == "expirado" ? DateTime.UtcNow.AddMinutes(-1) : DateTime.UtcNow.AddMinutes(5),
            new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private static Dictionary<string, object?> Body() => new()
    {
        ["plMaximo"] = 15m, ["dyMinimo"] = 6m, ["roeMinimo"] = 15m,
        ["dividaPatrimonioMaximo"] = 1m, ["margemLiquidaMinimo"] = 20m, ["retornoPreco"] = 6m
    };

    private static HttpRequestMessage CriarRequest(string metodo, int idUsuario, object? body = null) =>
        new(new HttpMethod(metodo), $"/api/usuarios/{idUsuario}/configuracao") { Content = JsonContent.Create(body ?? Body()) };

    private static async Task ProblemaSemDados(HttpResponseMessage response, int status)
    {
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var texto = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(texto);
        Assert.Equal(status, json.RootElement.GetProperty("status").GetInt32());
        Assert.True(json.RootElement.TryGetProperty("traceId", out _));
        foreach (var campo in new[] { "idConfiguracao", "idUsuario", "plMaximo", "dyMinimo", "roeMinimo", "dividaPatrimonioMaximo",
            "margemLiquidaMinimo", "retornoPreco", "dataInclusao", "dataAlteracao", "stackTrace", "exception" })
            Assert.False(json.RootElement.TryGetProperty(campo, out _));
        Assert.DoesNotContain("token-invalido-marcador", texto);
        Assert.DoesNotContain("ILCFG001", texto);
        if (status == 403) Assert.Equal("Forbidden", json.RootElement.GetProperty("title").GetString());
    }

    private static DateTime RepositorioData() => new(2030, 1, 2, 3, 4, 5);

    private sealed class ServicoObservado(RepositorioObservado repository) : IConfiguracaoService
    {
        private readonly ConfiguracaoService service = new(repository, new ConfiguracaoRequestValidator());
        public int Chamadas { get; private set; }
        public Task<ConfiguracaoResponse> ObterAsync(int idUsuario, CancellationToken cancellationToken)
        { Chamadas++; return service.ObterAsync(idUsuario, cancellationToken); }
        public Task<ConfiguracaoResponse> AdicionarAsync(int idUsuario, ConfiguracaoRequest request, CancellationToken cancellationToken)
        { Chamadas++; return service.AdicionarAsync(idUsuario, request, cancellationToken); }
        public Task<ConfiguracaoResponse> AlterarAsync(int idUsuario, ConfiguracaoRequest request, CancellationToken cancellationToken)
        { Chamadas++; return service.AlterarAsync(idUsuario, request, cancellationToken); }
    }

    private sealed class RepositorioObservado : IConfiguracaoRepository
    {
        public Dictionary<int, ConfiguracaoResponse> Registros { get; } = [];
        public List<int> UsuariosConsultados { get; } = [];
        public int Chamadas => UsuariosConsultados.Count;
        public int Inclusoes { get; private set; }
        public int Alteracoes { get; private set; }
        public ConfiguracaoRequest? UltimoRequest { get; private set; }
        public void Preparar(int idUsuario) => Registros[idUsuario] = Criar(idUsuario,
            new() { PLMaximo = 15, DYMinimo = 6, ROEMinimo = 15, DividaPatrimonioMaximo = 1, MargemLiquidaMinimo = 20, RetornoPreco = 6 }, null);
        public Task<ConfiguracaoResponse?> ObterAsync(int idUsuario, CancellationToken cancellationToken)
        {
            UsuariosConsultados.Add(idUsuario);
            return Task.FromResult(Registros.GetValueOrDefault(idUsuario));
        }
        public Task AdicionarAsync(int idUsuario, ConfiguracaoRequest request, CancellationToken cancellationToken)
        {
            UsuariosConsultados.Add(idUsuario);
            Inclusoes++;
            if (Registros.ContainsKey(idUsuario)) throw new ConfiguracaoJaExisteException();
            UltimoRequest = request;
            Registros.Add(idUsuario, Criar(idUsuario, request, null));
            return Task.CompletedTask;
        }
        public Task AlterarAsync(int idUsuario, ConfiguracaoRequest request, CancellationToken cancellationToken)
        {
            UsuariosConsultados.Add(idUsuario);
            Alteracoes++;
            if (!Registros.ContainsKey(idUsuario)) throw new ConfiguracaoNaoEncontradaException();
            UltimoRequest = request;
            Registros[idUsuario] = Criar(idUsuario, request, new DateTime(2030, 1, 3));
            return Task.CompletedTask;
        }
        private static ConfiguracaoResponse Criar(int idUsuario, ConfiguracaoRequest request, DateTime? alteracao) => new()
        {
            IdUsuario = idUsuario, IdConfiguracao = idUsuario + 100, DataInclusao = RepositorioData(), DataAlteracao = alteracao,
            PLMaximo = request.PLMaximo!.Value, DYMinimo = request.DYMinimo!.Value, ROEMinimo = request.ROEMinimo!.Value,
            DividaPatrimonioMaximo = request.DividaPatrimonioMaximo!.Value, MargemLiquidaMinimo = request.MargemLiquidaMinimo!.Value,
            RetornoPreco = request.RetornoPreco!.Value
        };
    }
}
