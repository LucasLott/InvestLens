using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using InvestLens.Api;
using InvestLens.Application.DTOs.Configuracao;
using InvestLens.Application.Exceptions;
using InvestLens.Application.Interfaces.Repositories.Configuracao;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InvestLens.IntegrationTests
{
    public class ConfiguracaoEndpointTests
    {
        [Theory]
        [InlineData("GET", "propria", 200)]
        [InlineData("POST", "propria", 201)]
        [InlineData("PUT", "propria", 200)]
        [InlineData("GET", "outro", 403)]
        [InlineData("POST", "outro", 403)]
        [InlineData("PUT", "outro", 403)]
        [InlineData("GET", "sem-token", 401)]
        [InlineData("POST", "sem-token", 401)]
        [InlineData("PUT", "sem-token", 401)]
        public async Task EndpointsExigemBearerESameUser(string method, string cenario, int statusEsperado)
        {
            var repository = new ConfiguracaoDouble { Cenario = method != "POST" && cenario == "propria" ? "existente" : null };
            if (repository.Cenario == "existente") repository.Preparar(15);
            await using var factory = CriarFactory(repository);
            using var client = factory.CreateClient();
            if (cenario != "sem-token") client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", factory.Services.GetRequiredService<InvestLens.Application.Interfaces.Security.IJwtTokenGenerator>()
                    .Generate(cenario == "propria" ? 15 : 27, "015", "Usuário").AccessToken);

            using var response = await EnviarAsync(client, method, "/api/usuarios/15/configuracao", Request());

            Assert.Equal(statusEsperado, (int)response.StatusCode);
            var chamadasEsperadas = statusEsperado == 200 && method == "GET" ? 1
                : statusEsperado is 200 or 201 ? 2 : 0;
            Assert.Equal(chamadasEsperadas, repository.Calls);
            if (statusEsperado is 401 or 403) await ProblemaSeguro(response, statusEsperado);
        }

        [Theory]
        [InlineData("GET", "nao-encontrada", 404)]
        [InlineData("PUT", "nao-encontrada", 404)]
        [InlineData("POST", "conflito", 409)]
        public async Task RetornaStatusSemanticoParaAusenciaEConflito(string method, string cenario, int statusEsperado)
        {
            var repository = new ConfiguracaoDouble { Cenario = cenario };
            await using var factory = CriarFactory(repository);
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                factory.Services.GetRequiredService<InvestLens.Application.Interfaces.Security.IJwtTokenGenerator>().Generate(15, "015", "Usuário").AccessToken);

            using var response = await EnviarAsync(client, method, "/api/usuarios/15/configuracao", Request());

            Assert.Equal(statusEsperado, (int)response.StatusCode);
            await ProblemaSeguro(response, statusEsperado);
        }

        [Theory]
        [InlineData("plMaximo", -0.0001)]
        [InlineData("dyMinimo", -0.0001)]
        [InlineData("roeMinimo", -0.0001)]
        [InlineData("dividaPatrimonioMaximo", -0.0001)]
        [InlineData("margemLiquidaMinimo", -0.0001)]
        [InlineData("retornoPreco", 0)]
        [InlineData("plMaximo", 10000)]
        public async Task RejeitaCriteriosInvalidosAntesDoRepository(string propriedade, decimal valor)
        {
            var repository = new ConfiguracaoDouble();
            await using var factory = CriarFactory(repository);
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                factory.Services.GetRequiredService<InvestLens.Application.Interfaces.Security.IJwtTokenGenerator>().Generate(15, "015", "Usuário").AccessToken);
            var body = new Dictionary<string, decimal> { ["plMaximo"] = 15, ["dyMinimo"] = 6, ["roeMinimo"] = 15,
                ["dividaPatrimonioMaximo"] = 1, ["margemLiquidaMinimo"] = 20, ["retornoPreco"] = 6 };
            body[propriedade] = valor;

            using var response = await client.PostAsJsonAsync("/api/usuarios/15/configuracao", body);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(0, repository.Calls);
            await ProblemaSeguro(response, 400);
        }

        [Fact]
        public async Task CriaEAtualizaComContratoSeguroSemIdNoBody()
        {
            var repository = new ConfiguracaoDouble();
            await using var factory = CriarFactory(repository);
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                factory.Services.GetRequiredService<InvestLens.Application.Interfaces.Security.IJwtTokenGenerator>().Generate(15, "015", "Usuário").AccessToken);

            using var created = await client.PostAsJsonAsync("/api/usuarios/15/configuracao", Request());
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            Assert.Equal("/api/usuarios/15/configuracao", created.Headers.Location?.AbsolutePath);
            await ValidarResponse(created, 15, 15, null);

            using var updated = await client.PutAsJsonAsync("/api/usuarios/15/configuracao", new
            {
                plMaximo = 16m, dyMinimo = 7m, roeMinimo = 17m, dividaPatrimonioMaximo = 2m,
                margemLiquidaMinimo = 21m, retornoPreco = 8m
            });
            Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
            await ValidarResponse(updated, 15, 16, repository.DataAlteracao);
            Assert.Equal(4, repository.Calls);
        }

        private static WebApplicationFactory<Program> CriarFactory(ConfiguracaoDouble repository) =>
            new TestApiFactory().WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            {
                services.RemoveAll<IConfiguracaoRepository>();
                services.AddSingleton<IConfiguracaoRepository>(repository);
            }));

        private static object Request() => new
        {
            plMaximo = 15m, dyMinimo = 6m, roeMinimo = 15m, dividaPatrimonioMaximo = 1m,
            margemLiquidaMinimo = 20m, retornoPreco = 6m
        };

        private static Task<HttpResponseMessage> EnviarAsync(HttpClient client, string method, string uri, object request) => method switch
        {
            "GET" => client.GetAsync(uri),
            "POST" => client.PostAsJsonAsync(uri, request),
            "PUT" => client.PutAsJsonAsync(uri, request),
            _ => throw new InvalidOperationException()
        };

        private static async Task ProblemaSeguro(HttpResponseMessage response, int status)
        {
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(status, json.RootElement.GetProperty("status").GetInt32());
            Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("traceId").GetString()));
        }

        private static async Task ValidarResponse(HttpResponseMessage response, int idUsuario, decimal plMaximo, DateTime? dataAlteracao)
        {
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(new[] { "dataAlteracao", "dataInclusao", "dividaPatrimonioMaximo", "dyMinimo", "idConfiguracao", "idUsuario", "margemLiquidaMinimo", "plMaximo", "retornoPreco", "roeMinimo" },
                json.RootElement.EnumerateObject().Select(x => x.Name).Order().ToArray());
            Assert.Equal(idUsuario, json.RootElement.GetProperty("idUsuario").GetInt32());
            Assert.Equal(plMaximo, json.RootElement.GetProperty("plMaximo").GetDecimal());
            var dataAlteracaoJson = json.RootElement.GetProperty("dataAlteracao");
            if (dataAlteracao is null) Assert.Equal(JsonValueKind.Null, dataAlteracaoJson.ValueKind);
            else Assert.Equal(dataAlteracao, dataAlteracaoJson.GetDateTime());
        }

        private sealed class ConfiguracaoDouble : IConfiguracaoRepository
        {
            private ConfiguracaoResponse? configuracao;
            public int Calls { get; private set; }
            public string? Cenario { get; init; }
            public DateTime? DataAlteracao { get; private set; }

            public void Preparar(int idUsuario) => configuracao = Criar(idUsuario, new ConfiguracaoRequest
            {
                PLMaximo = 15, DYMinimo = 6, ROEMinimo = 15, DividaPatrimonioMaximo = 1,
                MargemLiquidaMinimo = 20, RetornoPreco = 6
            }, null);

            public Task<ConfiguracaoResponse?> ObterAsync(int idUsuario, CancellationToken cancellationToken)
            {
                Calls++;
                if (Cenario == "nao-encontrada") return Task.FromResult<ConfiguracaoResponse?>(null);
                return Task.FromResult(configuracao?.IdUsuario == idUsuario ? configuracao : null);
            }

            public Task AdicionarAsync(int idUsuario, ConfiguracaoRequest request, CancellationToken cancellationToken)
            {
                Calls++;
                if (Cenario == "conflito" || configuracao is not null) throw new ConfiguracaoJaExisteException();
                configuracao = Criar(idUsuario, request, null);
                return Task.CompletedTask;
            }

            public Task AlterarAsync(int idUsuario, ConfiguracaoRequest request, CancellationToken cancellationToken)
            {
                Calls++;
                if (Cenario == "nao-encontrada" || configuracao?.IdUsuario != idUsuario) throw new ConfiguracaoNaoEncontradaException();
                DataAlteracao = DateTime.UtcNow;
                configuracao = Criar(idUsuario, request, DataAlteracao);
                return Task.CompletedTask;
            }

            private static ConfiguracaoResponse Criar(int idUsuario, ConfiguracaoRequest request, DateTime? dataAlteracao) => new()
            {
                IdConfiguracao = 7, IdUsuario = idUsuario, PLMaximo = request.PLMaximo!.Value,
                DYMinimo = request.DYMinimo!.Value, ROEMinimo = request.ROEMinimo!.Value,
                DividaPatrimonioMaximo = request.DividaPatrimonioMaximo!.Value,
                MargemLiquidaMinimo = request.MargemLiquidaMinimo!.Value, RetornoPreco = request.RetornoPreco!.Value,
                DataInclusao = new DateTime(2030, 1, 2, 3, 4, 5, DateTimeKind.Utc), DataAlteracao = dataAlteracao
            };
        }
    }
}
