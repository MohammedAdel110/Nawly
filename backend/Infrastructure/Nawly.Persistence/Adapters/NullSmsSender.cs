using Microsoft.Extensions.Logging;
using Nawly.Abstraction.IServices.Sms;
using Nawly.Shared.Common.Results;

namespace Nawly.Persistence.Adapters;

/// <summary>
/// Development/fallback SMS sender that logs dispatched templates instead of calling an external gateway.
/// Replace with Twilio/Vodafone/Unifonic adapter in production via DI registration before AddPersistenceServices.
/// </summary>
public sealed class NullSmsSender(ILogger<NullSmsSender> logger) : ISmsSender
{
    public Task<Result> SendAsync(
        string phoneNumber,
        string templateId,
        IReadOnlyDictionary<string, string> parameters,
        string purpose,
        CancellationToken ct = default)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            if (parameters.TryGetValue("code", out var otpCode) && !string.IsNullOrWhiteSpace(otpCode))
            {
                logger.LogInformation(
                    "[Dev Fake SMS] Sent template '{TemplateKey}' ({Purpose}) to {PhoneNumber} | OTP Code: {OtpCode}",
                    templateId,
                    purpose,
                    phoneNumber,
                    otpCode);
            }
            else
            {
                var keys = parameters.Count == 0 ? "<none>" : string.Join(", ", parameters.Keys);
                logger.LogInformation(
                    "[NullSmsSender] Purpose={Purpose}, Template={Template}, Phone={Phone}, ParamKeys=[{Keys}]",
                    purpose,
                    templateId,
                    phoneNumber,
                    keys);
            }
        }

        return Task.FromResult(Result.Success());
    }
}
