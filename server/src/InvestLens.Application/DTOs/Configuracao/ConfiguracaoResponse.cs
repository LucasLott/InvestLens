namespace InvestLens.Application.DTOs.Configuracao
{
    public sealed class ConfiguracaoResponse
    {
        public int IdConfiguracao { get; init; }
        public int IdUsuario { get; init; }
        public decimal PLMaximo { get; init; }
        public decimal DYMinimo { get; init; }
        public decimal ROEMinimo { get; init; }
        public decimal DividaPatrimonioMaximo { get; init; }
        public decimal MargemLiquidaMinimo { get; init; }
        public decimal RetornoPreco { get; init; }
        public DateTime DataInclusao { get; init; }
        public DateTime? DataAlteracao { get; init; }
    }
}
