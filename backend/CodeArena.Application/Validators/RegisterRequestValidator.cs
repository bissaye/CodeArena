using CodeArena.Application.DTOs;
using FluentValidation;

namespace CodeArena.Application.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Le pseudonyme est obligatoire.")
            .Length(3, 30).WithMessage("Le pseudonyme doit contenir entre 3 et 30 caractères.")
            .Matches(@"^[a-zA-Z0-9\-]+$").WithMessage("Le pseudonyme ne peut contenir que des lettres, chiffres et tirets.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Le mot de passe est obligatoire.")
            .MinimumLength(8).WithMessage("Le mot de passe doit contenir au moins 8 caractères.");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Le pays est obligatoire.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("L'adresse email est invalide.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20).WithMessage("Le numéro de téléphone ne peut pas dépasser 20 caractères.")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

        RuleFor(x => x.Region)
            .MaximumLength(100).WithMessage("La région ne peut pas dépasser 100 caractères.")
            .When(x => !string.IsNullOrWhiteSpace(x.Region));

        RuleFor(x => x.School)
            .MaximumLength(200).WithMessage("Le nom de l'école ne peut pas dépasser 200 caractères.")
            .When(x => !string.IsNullOrWhiteSpace(x.School));
    }
}
