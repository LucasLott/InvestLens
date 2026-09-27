namespace InvestLens.Application.DTOs.Configuracao
{
    public sealed class ConfiguracaoRequest
    {
        public decimal? PLMaximo { get; init; }
        public decimal? DYMinimo { get; init; }
        public decimal? ROEMinimo { get; init; }
        public decimal? DividaPatrimonioMaximo { get; init; }
        public decimal? MargemLiquidaMinimo { get; init; }
        public decimal? RetornoPreco { get; init; }
    }
}
