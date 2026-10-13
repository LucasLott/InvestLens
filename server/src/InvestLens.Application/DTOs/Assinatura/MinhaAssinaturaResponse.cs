namespace InvestLens.Application.DTOs.Assinatura
{
    public sealed record MinhaAssinaturaResponse(string PlanoCadastrado,
                                                 string PlanoEfetivo,
                                                 string? StatusAssinatura,
                                                 DateTimeOffset? InicioVigencia,
                                                 DateTimeOffset? FimVigencia,
                                                 bool AcessoPremiumAtivo);
}