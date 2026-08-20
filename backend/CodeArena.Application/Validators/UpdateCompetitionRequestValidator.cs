using CodeArena.Application.DTOs;
using FluentValidation;

namespace CodeArena.Application.Validators;

public class UpdateCompetitionRequestValidator : AbstractValidator<UpdateCompetitionRequest>
{
    public UpdateCompetitionRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Le nom est obligatoire.")
            .MaximumLength(200).WithMessage("Le nom ne peut pas dépasser 200 caractères.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("La date de début est obligatoire.");

        RuleFor(x => x).Must(x => x.DurationHours > 0 || x.DurationMinutes > 0)
            .WithMessage("La durée doit être supérieure à zéro.");

        RuleFor(x => x.DurationHours)
            .GreaterThanOrEqualTo(0).WithMessage("Les heures ne peuvent pas être négatives.");

        RuleFor(x => x.DurationMinutes)
            .InclusiveBetween(0, 59).WithMessage("Les minutes doivent être entre 0 et 59.");
    }
}
