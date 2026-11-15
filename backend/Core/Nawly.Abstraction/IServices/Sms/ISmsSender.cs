using Nawly.Shared.Common.Results;

namespace Nawly.Abstraction.IServices.Sms;

public interface ISmsSender
{
    Task<Result> SendAsync(
        string phoneNumber,
        string templateId,
        IReadOnlyDictionary<string, string> parameters,
        string purpose,
        CancellationToken ct = default);
}
