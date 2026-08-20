using CodeArena.Application.DTOs;
using FluentValidation;

namespace CodeArena.Application.Validators;

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Le pays est obligatoire.")
            .MaximumLength(100);

        RuleFor(x => x.Region)
            .MaximumLength(100).When(x => x.Region is not null);

        RuleFor(x => x.School)
            .MaximumLength(200).When(x => x.School is not null);
    }
}
