using FluentValidation;
using InvestLens.Application.DTOs.Auth;

namespace InvestLens.Application.Validators
{
    public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("O email é obrigatório.")
                .EmailAddress().WithMessage("O email deve ser válido.")
                .MaximumLength(255).WithMessage("O email não pode exceder 255 caracteres.");

            RuleFor(x => x.Senha)
                .NotEmpty().WithMessage("A senha é obrigatória.");
        }
    }
}
