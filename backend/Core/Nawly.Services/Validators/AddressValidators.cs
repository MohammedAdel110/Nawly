using FluentValidation;
using Nawly.Domain.ValueObjects;
using Nawly.Shared.DTOs.Address;

namespace Nawly.Services.Validators;

public sealed class CreateAddressDtoValidator : AbstractValidator<CreateAddressDto>
{
    public CreateAddressDtoValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(50);
        RuleFor(x => x.AreaId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Building).MaximumLength(50);
        RuleFor(x => x.Floor).MaximumLength(20);
        RuleFor(x => x.Apartment).MaximumLength(20);
        RuleFor(x => x.Notes).MaximumLength(300);

        RuleFor(x => x.ContactPhone)
            .NotEmpty()
            .Must(p => PhoneNumber.TryParseEgyptianMobile(p, out _))
            .WithMessage("Contact phone must be a valid Egyptian mobile number.");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90d, 90d)
            .When(x => x.Latitude.HasValue);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180d, 180d)
            .When(x => x.Longitude.HasValue);
    }
}

public sealed class UpdateAddressDtoValidator : AbstractValidator<UpdateAddressDto>
{
    public UpdateAddressDtoValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(50);
        RuleFor(x => x.AreaId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Building).MaximumLength(50);
        RuleFor(x => x.Floor).MaximumLength(20);
        RuleFor(x => x.Apartment).MaximumLength(20);
        RuleFor(x => x.Notes).MaximumLength(300);

        RuleFor(x => x.ContactPhone)
            .NotEmpty()
            .Must(p => PhoneNumber.TryParseEgyptianMobile(p, out _))
            .WithMessage("Contact phone must be a valid Egyptian mobile number.");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90d, 90d)
            .When(x => x.Latitude.HasValue);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180d, 180d)
            .When(x => x.Longitude.HasValue);
    }
}
