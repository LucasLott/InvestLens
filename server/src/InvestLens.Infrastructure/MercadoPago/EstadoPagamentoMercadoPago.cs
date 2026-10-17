namespace InvestLens.Infrastructure.MercadoPago;

// Apenas classifica respostas de GET /v1/payments/{id}; não autoriza acesso.
public enum EstadoPagamentoMercadoPago
{
    Desconhecido,
    Pendente,
    Aprovado,
    Recusado,
    Cancelado,
    Reembolsado,
    ReembolsoParcial,
    Contestacao
}

public static class ClassificadorPagamentoMercadoPago
{
    public static EstadoPagamentoMercadoPago Classificar(string? status, string? detalhe) => status switch
    {
        "approved" when detalhe == "accredited" => EstadoPagamentoMercadoPago.Aprovado,
        "approved" when detalhe == "partially_refunded" => EstadoPagamentoMercadoPago.ReembolsoParcial,
        "pending" or "in_process" or "authorized" => EstadoPagamentoMercadoPago.Pendente,
        "rejected" => EstadoPagamentoMercadoPago.Recusado,
        "cancelled" => EstadoPagamentoMercadoPago.Cancelado,
        "refunded" => EstadoPagamentoMercadoPago.Reembolsado,
        "charged_back" or "in_mediation" => EstadoPagamentoMercadoPago.Contestacao,
        _ => EstadoPagamentoMercadoPago.Desconhecido
    };
}
