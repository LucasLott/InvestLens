using FluentValidation;
using InvestLens.Application.DTOs.Configuracao;

namespace InvestLens.Application.Validators
{
    public sealed class ConfiguracaoRequestValidator : AbstractValidator<ConfiguracaoRequest>
    {
        public ConfiguracaoRequestValidator()
        {
            RuleFor(x => x.PLMaximo).NotNull().GreaterThanOrEqualTo(0).PrecisionScale(8, 4, true);
            RuleFor(x => x.DYMinimo).NotNull().GreaterThanOrEqualTo(0).PrecisionScale(8, 4, true);
            RuleFor(x => x.ROEMinimo).NotNull().GreaterThanOrEqualTo(0).PrecisionScale(8, 4, true);
            RuleFor(x => x.DividaPatrimonioMaximo).NotNull().GreaterThanOrEqualTo(0).PrecisionScale(8, 4, true);
            RuleFor(x => x.MargemLiquidaMinimo).NotNull().GreaterThanOrEqualTo(0).PrecisionScale(8, 4, true);
            RuleFor(x => x.RetornoPreco).NotNull().GreaterThan(0).PrecisionScale(8, 4, true);
        }
    }
}
