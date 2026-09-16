using FluentValidation;
using InvestLens.Application.DTOs.Usuario;

namespace InvestLens.Application.Validators
{
    public class AdicionarUsuarioRequestValidator : AbstractValidator<AdicionarUsuarioRequest>
    {
        public AdicionarUsuarioRequestValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("O nome é obrigatório.")
                .MaximumLength(80).WithMessage("O nome não pode exceder 80 caracteres.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("O email é obrigatório.")
                .EmailAddress().WithMessage("O email deve ser válido.");

            RuleFor(x => x.Senha)
                .NotEmpty().WithMessage("A senha é obrigatória.")
                .MinimumLength(6).WithMessage("A senha deve ter no mínimo 6 caracteres.");
        }
    }
}