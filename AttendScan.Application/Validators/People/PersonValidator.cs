using AttendScan.Application.Dtos.People;
using FluentValidation;

namespace AttendScan.Application.Validators.People;

public class PersonValidator : AbstractValidator<PersonDto>
{
    public PersonValidator()
    {
        RuleFor(x => x.PaternalSurname)
            .NotEmpty().WithMessage("PaternalSurname is required.")
            .MaximumLength(80).WithMessage("PaternalSurname must not exceed 80 characters.");

        RuleFor(x => x.MaternalSurname)
            .MaximumLength(80).WithMessage("MaternalSurname must not exceed 80 characters.");

        RuleFor(x => x.GivenNames)
            .NotEmpty().WithMessage("GivenNames is required.")
            .MaximumLength(120).WithMessage("GivenNames must not exceed 120 characters.");

        RuleFor(x => x.Region).MaximumLength(50).WithMessage("Region must not exceed 50 characters.");
        RuleFor(x => x.District).MaximumLength(100).WithMessage("District must not exceed 100 characters.");
        RuleFor(x => x.Church).MaximumLength(150).WithMessage("Church must not exceed 150 characters.");
        RuleFor(x => x.Position).MaximumLength(200).WithMessage("Position must not exceed 200 characters.");
        RuleFor(x => x.DelegateType).MaximumLength(50).WithMessage("DelegateType must not exceed 50 characters.");

        RuleFor(x => x.ListNumber)
            .GreaterThan(0).When(x => x.ListNumber.HasValue).WithMessage("ListNumber must be greater than 0.");

        RuleFor(x => x.Age)
            .InclusiveBetween(1, 120).When(x => x.Age.HasValue).WithMessage("Age must be between 1 and 120.");

        RuleFor(x => x.Gender)
            .Must(g => g is null or "M" or "F").WithMessage("Gender must be M or F.");
    }
}
