using Nawly.Shared.Common.Results;
using Nawly.Shared.DTOs.Attachment;

namespace Nawly.Abstraction.IServices.Attachment;

public interface IAttachmentService
{
    Task<Result<UploadedFileDto>> UploadImageAsync(
        Stream content,
        string fileName,
        string contentType,
        long sizeBytes,
        string folder,
        CancellationToken ct = default);

    Task<Result<string>> GetDownloadUrlAsync(string storageKey, CancellationToken ct = default);
}
