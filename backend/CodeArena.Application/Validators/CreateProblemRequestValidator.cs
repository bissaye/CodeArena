using CodeArena.Application.DTOs;
using FluentValidation;

namespace CodeArena.Application.Validators;

public class CreateProblemRequestValidator : AbstractValidator<CreateProblemRequest>
{
    public CreateProblemRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Le titre est obligatoire.")
            .MaximumLength(200).WithMessage("Le titre ne peut pas dépasser 200 caractères.");

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("L'énoncé est obligatoire.");

        RuleFor(x => x.Points)
            .GreaterThan(0).WithMessage("Les points doivent être supérieurs à zéro.");
    }
}
