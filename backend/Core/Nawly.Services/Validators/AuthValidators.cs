using FluentValidation;
using Nawly.Domain.ValueObjects;
using Nawly.Shared.DTOs.Auth.Login;
using Nawly.Shared.DTOs.Auth.Otp;

namespace Nawly.Services.Validators;

public sealed class RequestOtpDtoValidator : AbstractValidator<RequestOtpDto>
{
    public RequestOtpDtoValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .Must(p => PhoneNumber.TryParseEgyptianMobile(p, out _))
            .WithMessage("Phone number must be a valid Egyptian mobile number.");

        RuleFor(x => x.Purpose).IsInEnum();
    }
}

public sealed class VerifyOtpDtoValidator : AbstractValidator<VerifyOtpDto>
{
    public VerifyOtpDtoValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .Must(p => PhoneNumber.TryParseEgyptianMobile(p, out _))
            .WithMessage("Phone number must be a valid Egyptian mobile number.");

        RuleFor(x => x.Code)
            .NotEmpty()
            .Length(6)
            .Matches("^[0-9]{6}$")
            .WithMessage("OTP code must be 6 numeric digits.");

        RuleFor(x => x.Purpose).IsInEnum();
    }
}

public sealed class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestDtoValidator()
    {
        RuleFor(x => x.Identifier)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128);
    }
}
