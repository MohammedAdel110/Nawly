using FluentValidation;
using Nawly.Domain.ValueObjects;
using Nawly.Shared.DTOs.Courier;
using Nawly.Shared.DTOs.Delivery;

namespace Nawly.Services.Validators;

public sealed class ApplyCourierDtoValidator : AbstractValidator<ApplyCourierDto>
{
    public ApplyCourierDtoValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .Must(p => PhoneNumber.TryParseEgyptianMobile(p, out _))
            .WithMessage("Phone number must be a valid Egyptian mobile number.");

        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(x => x.VehicleType).IsInEnum();
        RuleFor(x => x.NationalIdImageRef).NotEmpty().MaximumLength(300);
        RuleFor(x => x.PhotoRef).MaximumLength(300);
    }
}

public sealed class CourierLocationUpdateDtoValidator : AbstractValidator<CourierLocationUpdateDto>
{
    public CourierLocationUpdateDtoValidator()
    {
        RuleFor(x => x.Latitude).InclusiveBetween(-90d, 90d);
        RuleFor(x => x.Longitude).InclusiveBetween(-180d, 180d);
        RuleFor(x => x.Heading).InclusiveBetween(0d, 360d).When(x => x.Heading.HasValue);
        RuleFor(x => x.Speed).GreaterThanOrEqualTo(0d).When(x => x.Speed.HasValue);
    }
}

public sealed class ConfirmDeliveryCodeDtoValidator : AbstractValidator<ConfirmDeliveryCodeDto>
{
    public ConfirmDeliveryCodeDtoValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .Length(4)
            .Matches("^[0-9]{4}$")
            .WithMessage("Delivery confirmation code must be 4 digits.");

        RuleFor(x => x.CodCollectedAmount)
            .GreaterThanOrEqualTo(0m)
            .When(x => x.CodCollectedAmount.HasValue);
    }
}

public sealed class FailDeliveryDtoValidator : AbstractValidator<FailDeliveryDto>
{
    public FailDeliveryDtoValidator()
    {
        RuleFor(x => x.ReasonCode).IsInEnum();
        RuleFor(x => x.Note).MaximumLength(500);
    }
}
