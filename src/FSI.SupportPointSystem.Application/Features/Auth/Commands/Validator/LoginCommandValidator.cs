using FluentValidation;
using FSI.SupportPointSystem.Application.Features.Auth.Commands.Login;

namespace FSI.SupportPointSystem.Application.Features.Auth.Commands.Validator
{
    public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.Cpf)
                .NotEmpty().WithMessage("CPF é obrigatório.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Senha é obrigatória.");
        }
    }
}
