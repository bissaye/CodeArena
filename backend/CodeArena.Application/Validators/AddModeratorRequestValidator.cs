using CodeArena.Application.DTOs;
using FluentValidation;

namespace CodeArena.Application.Validators;

public class AddModeratorRequestValidator : AbstractValidator<AddModeratorRequest>
{
    public AddModeratorRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Le pseudonyme est obligatoire.")
            .MinimumLength(3).WithMessage("Le pseudonyme doit contenir au moins 3 caractères.")
            .MaximumLength(30).WithMessage("Le pseudonyme ne peut pas dépasser 30 caractères.");
    }
}
