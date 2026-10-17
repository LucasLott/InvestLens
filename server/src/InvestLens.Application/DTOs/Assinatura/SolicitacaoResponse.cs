namespace InvestLens.Application.DTOs.Assinatura
{
    public sealed record SolicitacaoResponse(int IdSolicitacao, string Tipo, string Status,
        string PlanoOrigem, string? PlanoDestino, DateTime DataSolicitacao);

    public sealed record AlteracaoPlanoRequest(int IdPlanoDestino);
}