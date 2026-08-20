using CodeArena.Application.DTOs;
using FluentValidation;

namespace CodeArena.Application.Validators;

public class VerifyEmailRequestValidator : AbstractValidator<VerifyEmailRequest>
{
    public VerifyEmailRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Le token de vérification est requis.");
    }
}
