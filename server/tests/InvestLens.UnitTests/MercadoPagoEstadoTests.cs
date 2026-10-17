using InvestLens.Infrastructure.MercadoPago;

namespace InvestLens.UnitTests;

public class MercadoPagoEstadoTests
{
    [Theory]
    [InlineData("approved", "accredited", EstadoPagamentoMercadoPago.Aprovado)]
    [InlineData("approved", "partially_refunded", EstadoPagamentoMercadoPago.ReembolsoParcial)]
    [InlineData("approved", "desconhecido", EstadoPagamentoMercadoPago.Desconhecido)]
    [InlineData("authorized", "pending_capture", EstadoPagamentoMercadoPago.Pendente)]
    [InlineData("pending", "pending_waiting_payment", EstadoPagamentoMercadoPago.Pendente)]
    [InlineData("in_process", "pending_review_manual", EstadoPagamentoMercadoPago.Pendente)]
    [InlineData("rejected", "bank_error", EstadoPagamentoMercadoPago.Recusado)]
    [InlineData("cancelled", "expired", EstadoPagamentoMercadoPago.Cancelado)]
    [InlineData("refunded", "refunded", EstadoPagamentoMercadoPago.Reembolsado)]
    [InlineData("charged_back", "in_process", EstadoPagamentoMercadoPago.Contestacao)]
    [InlineData("in_mediation", "pending", EstadoPagamentoMercadoPago.Contestacao)]
    [InlineData("novo_status", null, EstadoPagamentoMercadoPago.Desconhecido)]
    [InlineData(null, null, EstadoPagamentoMercadoPago.Desconhecido)]
    public void ClassificaEstadosSemConcederAcesso(string? status, string? detalhe, EstadoPagamentoMercadoPago esperado)
    {
        Assert.Equal(esperado, ClassificadorPagamentoMercadoPago.Classificar(status, detalhe));
    }
}
