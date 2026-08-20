using CodeArena.Application.DTOs;
using FluentValidation;

namespace CodeArena.Application.Validators;

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Le mot de passe actuel est requis.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Le nouveau mot de passe est requis.")
            .MinimumLength(8).WithMessage("Le mot de passe doit contenir au moins 8 caractères.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword).WithMessage("Les mots de passe ne correspondent pas.");
    }
}
