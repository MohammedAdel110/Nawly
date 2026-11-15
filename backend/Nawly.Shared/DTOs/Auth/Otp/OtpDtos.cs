using Nawly.Shared.EnumHelper.UserEnums;

namespace Nawly.Shared.DTOs.Auth.Otp;

public sealed record RequestOtpDto(string PhoneNumber, OtpPurpose Purpose = OtpPurpose.Login, string? CaptchaToken = null);

public sealed record RequestOtpResponseDto(DateTimeOffset ExpiresAt, int CooldownSeconds);

public sealed record VerifyOtpDto(string PhoneNumber, string Code, OtpPurpose Purpose = OtpPurpose.Login, string? DeviceName = null);
