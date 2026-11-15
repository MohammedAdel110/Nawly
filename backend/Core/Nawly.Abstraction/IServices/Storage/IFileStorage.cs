using Nawly.Shared.Common.Results;
using Nawly.Shared.DTOs.Attachment;

namespace Nawly.Abstraction.IServices.Storage;

public interface IFileStorage
{
    Task<Result<UploadedFileDto>> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        string folder,
        CancellationToken ct = default);

    Task<Result> DeleteAsync(string storageKey, CancellationToken ct = default);

    Task<Result<string>> GetSignedUrlAsync(string storageKey, TimeSpan validity, CancellationToken ct = default);
}
