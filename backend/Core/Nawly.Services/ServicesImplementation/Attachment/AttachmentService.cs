using Nawly.Abstraction.IServices.Attachment;
using Nawly.Abstraction.IServices.Storage;
using Nawly.Shared.Common.Results;
using Nawly.Shared.DTOs.Attachment;

namespace Nawly.Services.ServicesImplementation.Attachment;

public sealed class AttachmentService(IFileStorage fileStorage) : IAttachmentService
{
    public const long MaxImageBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    public Task<Result<UploadedFileDto>> UploadImageAsync(
        Stream content,
        string fileName,
        string contentType,
        long sizeBytes,
        string folder,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return Task.FromResult(Result.Failure<UploadedFileDto>(Error.Validation("attachment.invalid_name", "File name is required.")));

        if (sizeBytes <= 0 || sizeBytes > MaxImageBytes)
            return Task.FromResult(Result.Failure<UploadedFileDto>(Error.Validation("attachment.invalid_size", "Image size must be between 1 byte and 5 MB.")));

        if (!AllowedImageContentTypes.Contains(contentType))
            return Task.FromResult(Result.Failure<UploadedFileDto>(Error.Validation("attachment.unsupported_type", "Only JPEG, PNG, and WebP images are supported.")));

        var sanitizedFolder = string.IsNullOrWhiteSpace(folder) ? "uploads" : folder.Trim().ToLowerInvariant();
        return fileStorage.UploadAsync(content, fileName, contentType, sanitizedFolder, ct);
    }

    public Task<Result<string>> GetDownloadUrlAsync(string storageKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            return Task.FromResult(Result.Failure<string>(Error.Validation("attachment.invalid_key", "Storage key is required.")));

        return fileStorage.GetSignedUrlAsync(storageKey, TimeSpan.FromMinutes(15), ct);
    }
}
