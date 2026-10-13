namespace InvestLens.Application.Models
{
    public sealed class AssinaturaAtual
    {
        public string PlanoCadastrado { get; init; } = "Free";
        public string? PlanoAssinatura { get; init; }
        public string? StatusAssinatura { get; init; }
        public DateTimeOffset? InicioVigencia { get; init; }
        public DateTimeOffset? FimVigencia { get; init; }
    }
}