using System.Net.Http.Json;
using System.Text.Json;
using InvestLens.Api;
using InvestLens.Application.DTOs.Usuario;
using InvestLens.Application.Interfaces.Repositories.Usuario;
using InvestLens.Application.Models;
using InvestLens.Domain.Exceptions;
using InvestLens.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InvestLens.IntegrationTests
{
    public class UsuarioEndpointTests
    {
        [Theory]
        [InlineData("user@example.com", "password", "password", "success", 200)]
        [InlineData("invalid", "password", "password", "success", 400)]
        [InlineData("user@example.com", "short", "short", "success", 400)]
        [InlineData("user@example.com", "password", "different", "success", 400)]
        [InlineData("user@example.com", "password", "", "success", 400)]
        [InlineData("user@example.com", "password", "password", "business", 400)]
        [InlineData("user@example.com", "password", "password", "technical", 500)]
        public async Task RegistrationValidatesInputAndReturnsSafeResponses(
            string email, string senha, string confirmacaoSenha, string outcome, int expectedStatus)
        {
            var repository = new UsuarioDouble(outcome);
            await using var factory = new TestApiFactory().WithWebHostBuilder(builder =>
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IUsuarioRepository>();
                    services.AddSingleton<IUsuarioRepository>(repository);
                }));
            using var client = factory.CreateClient();
            using var response = await client.PostAsJsonAsync("/api/usuario",
                new { nome = "Usuário", email, senha, confirmacaoSenha });
            Assert.Equal(expectedStatus, (int)response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain(senha, body);
            Assert.DoesNotContain("$argon2id", body);
            Assert.DoesNotContain("sensitive-marker", body);
            if (expectedStatus == 200)
            {
                Assert.Equal(1, repository.Calls);
                Assert.NotNull(repository.Hash);
                Assert.True(await new PasswordHasher().Verify(senha, repository.Hash));
            }
            else
            {
                Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
                using var json = JsonDocument.Parse(body);
                Assert.Equal(expectedStatus, json.RootElement.GetProperty("status").GetInt32());
                Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("traceId").GetString()));
                Assert.Equal(outcome == "success" ? 0 : 1, repository.Calls);
            }
        }

        private sealed class UsuarioDouble(string outcome) : IUsuarioRepository
        {
            public int Calls { get; private set; }
            public string? Hash { get; private set; }
            public Task Adicionar(AdicionarUsuarioRequest request, string senhaHash, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Calls++;
                Hash = senhaHash;
                if (outcome == "business") throw new BusinessException();
                if (outcome == "technical") throw new InvalidOperationException("sensitive-marker");
                return Task.CompletedTask;
            }
            public Task<DadosLogin?> ObterDadosLoginAsync(string email, CancellationToken cancellationToken) =>
                throw new NotSupportedException();
        }
    }
}
