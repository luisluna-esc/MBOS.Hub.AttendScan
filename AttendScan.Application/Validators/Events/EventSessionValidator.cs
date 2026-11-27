using AttendScan.Application.Dtos.Events;
using FluentValidation;

namespace AttendScan.Application.Validators.Events;

public class EventSessionValidator : AbstractValidator<EventSessionDto>
{
    public EventSessionValidator()
    {
        RuleFor(x => x.EventId)
            .GreaterThan(0).WithMessage("EventId is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(x => x.SessionDate)
            .NotEmpty().WithMessage("SessionDate is required.");
    }
}
