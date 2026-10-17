using System.Net;
using System.Text;
using InvestLens.Infrastructure.MercadoPago;

namespace InvestLens.UnitTests;

public class MercadoPagoConsultaTests
{
    [Fact]
    public async Task ConsultaPagamentoUsaRecursoOficialEInterpretaResposta()
    {
        var handler = new FakeHandler();
        var gateway = new ConsultaPagamentoMercadoPago(new HttpClient(handler));
        var pagamento = await gateway.ObterAsync(123, "test-token", CancellationToken.None);

        Assert.Equal("https://api.mercadopago.com/v1/payments/123", handler.Uri);
        Assert.Equal("Bearer", handler.Scheme);
        Assert.Equal(123, pagamento.Id);
        Assert.Equal(49.90m, pagamento.TransactionAmount);
        Assert.False(pagamento.LiveMode);
        Assert.Equal(EstadoPagamentoMercadoPago.Aprovado, ClassificadorPagamentoMercadoPago.Classificar(pagamento.Status, pagamento.StatusDetail));
    }

    [Fact]
    public async Task FalhaHttpNaoEhConvertidaEmPagamentoAprovado()
    {
        var gateway = new ConsultaPagamentoMercadoPago(new HttpClient(new FakeHandler(HttpStatusCode.BadGateway)));
        await Assert.ThrowsAsync<HttpRequestException>(() => gateway.ObterAsync(123, "test-token", CancellationToken.None));
    }

    private sealed class FakeHandler(HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        public string? Uri { get; private set; }
        public string? Scheme { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Uri = request.RequestUri?.ToString();
            Scheme = request.Headers.Authorization?.Scheme;
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent("""{"id":123,"status":"approved","status_detail":"accredited","transaction_amount":49.90,"currency_id":"BRL","external_reference":"abc","collector_id":456,"live_mode":false}""", Encoding.UTF8, "application/json")
            });
        }
    }
}
