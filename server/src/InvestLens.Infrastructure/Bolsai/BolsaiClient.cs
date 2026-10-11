using System.Net;
using InvestLens.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InvestLens.Infrastructure.Bolsai
{
    /// <summary>Cliente técnico da bolsai. A resposta do diagnóstico não é desserializada nesta etapa.</summary>
    public sealed class BolsaiClient(HttpClient httpClient, IOptions<BolsaiSettings> settings, ILogger<BolsaiClient> logger)
    {
        private const string ApiKeyHeader = "X-API-Key";
        private const string ConnectivityPath = "stocks/?bdi_code=02&limit=1";
        private readonly HttpClient _httpClient = httpClient;
        private readonly BolsaiSettings _settings = settings.Value;
        private readonly ILogger<BolsaiClient> _logger = logger;

        /// <summary>
        /// Faz uma única chamada autenticada e descartável ao endpoint documentado de listagem de ações.
        /// Não interpreta nem persiste o corpo de resposta.
        /// </summary>
        public async Task VerifyConnectivityAsync(CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, ConnectivityPath);
            request.Headers.TryAddWithoutValidation(ApiKeyHeader, _settings.ApiKey);

            try
            {
                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Conectividade com a bolsai confirmada. Status HTTP {StatusCode}.", (int)response.StatusCode);
                    return;
                }

                throw CreateHttpException(response.StatusCode);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException exception)
            {
                throw LogAndCreate(BolsaiFailureKind.Timeout, "A chamada à bolsai excedeu o tempo limite configurado.", null, exception);
            }
            catch (HttpRequestException exception)
            {
                throw LogAndCreate(BolsaiFailureKind.NetworkUnavailable, "Não foi possível estabelecer comunicação com a bolsai.", null, exception);
            }
        }

        private BolsaiRequestException CreateHttpException(HttpStatusCode statusCode)
        {
            var (kind, message) = statusCode switch
            {
                HttpStatusCode.Unauthorized => (BolsaiFailureKind.Unauthorized, "A bolsai recusou a API Key informada."),
                HttpStatusCode.Forbidden => (BolsaiFailureKind.Forbidden, "A API Key não possui permissão para este recurso da bolsai."),
                HttpStatusCode.NotFound => (BolsaiFailureKind.NotFound, "O recurso solicitado não foi encontrado na bolsai."),
                HttpStatusCode.TooManyRequests => (BolsaiFailureKind.RateLimited, "O limite de requisições da bolsai foi atingido."),
                _ when (int)statusCode >= 500 => (BolsaiFailureKind.ExternalServiceFailure, "A bolsai apresentou uma falha externa."),
                _ => (BolsaiFailureKind.ExternalServiceFailure, "A bolsai retornou uma resposta não esperada.")
            };

            return LogAndCreate(kind, message, statusCode, null);
        }

        private BolsaiRequestException LogAndCreate(BolsaiFailureKind kind, string message, HttpStatusCode? statusCode, Exception? exception)
        {
            _logger.LogWarning("Falha na comunicação com a bolsai. Categoria {FailureKind}; status HTTP {StatusCode}; exceção técnica {ExceptionType}.", kind, statusCode is null ? null : (int)statusCode, exception?.GetType().Name);
            return new BolsaiRequestException(kind, message, statusCode, exception);
        }
    }
}
