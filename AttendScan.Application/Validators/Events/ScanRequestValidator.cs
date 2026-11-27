using AttendScan.Application.Dtos.Events;
using FluentValidation;

namespace AttendScan.Application.Validators.Events;

public class ScanRequestValidator : AbstractValidator<ScanRequestDto>
{
    public ScanRequestValidator()
    {
        RuleFor(x => x.EventSessionId)
            .GreaterThan(0).WithMessage("EventSessionId is required.");

        // Un QR que no es nuestro no es un error de la peticion: el servicio lo responde como
        // InvalidCode. Aqui solo se corta texto absurdo (un QR con una pagina entera).
        RuleFor(x => x.Code)
            .MaximumLength(512).WithMessage("Code must not exceed 512 characters.");
    }
}
