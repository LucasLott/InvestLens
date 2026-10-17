using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace InvestLens.Infrastructure.MercadoPago;

// Não é registrado na DI: a configuração segura e a identificação do ambiente
// devem ser validadas antes de conectar o adaptador ao processamento financeiro.
public sealed class ConsultaPagamentoMercadoPago(HttpClient client)
{
    public async Task<PagamentoMercadoPago> ObterAsync(long id, string accessToken, CancellationToken cancellationToken)
    {
        if (id <= 0 || string.IsNullOrWhiteSpace(accessToken)) throw new ArgumentException("Consulta inválida.");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.mercadopago.com/v1/payments/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PagamentoMercadoPago>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Resposta de pagamento vazia.");
    }
}

public sealed record PagamentoMercadoPago(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("status_detail")] string? StatusDetail,
    [property: JsonPropertyName("transaction_amount")] decimal TransactionAmount,
    [property: JsonPropertyName("currency_id")] string? CurrencyId,
    [property: JsonPropertyName("external_reference")] string? ExternalReference,
    [property: JsonPropertyName("collector_id")] long CollectorId,
    [property: JsonPropertyName("live_mode")] bool LiveMode);
