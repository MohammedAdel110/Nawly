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

    public async Task<Result<UploadedFileDto>> UploadImageAsync(
        Stream content,
        string fileName,
        string contentType,
        long sizeBytes,
        string folder,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return Result.Failure<UploadedFileDto>(Error.Validation("attachment.invalid_name", "File name is required."));

        if (sizeBytes <= 0 || sizeBytes > MaxImageBytes)
            return Result.Failure<UploadedFileDto>(Error.Validation("attachment.invalid_size", "Image size must be between 1 byte and 5 MB."));

        if (!AllowedImageContentTypes.Contains(contentType))
            return Result.Failure<UploadedFileDto>(Error.Validation("attachment.unsupported_type", "Only JPEG, PNG, and WebP images are supported."));

        Stream uploadStream = content;
        MemoryStream? bufferedStream = null;

        try
        {
            if (!content.CanSeek)
            {
                bufferedStream = new MemoryStream();
                await content.CopyToAsync(bufferedStream, ct);
                bufferedStream.Position = 0;
                uploadStream = bufferedStream;
            }

            var originalPosition = uploadStream.Position;
            var header = new byte[12];
            var totalRead = 0;
            while (totalRead < header.Length)
            {
                var read = await uploadStream.ReadAsync(header.AsMemory(totalRead, header.Length - totalRead), ct);
                if (read == 0)
                    break;
                totalRead += read;
            }

            uploadStream.Position = originalPosition;

            if (!HasValidImageSignature(contentType, header, totalRead))
            {
                return Result.Failure<UploadedFileDto>(Error.Validation(
                    "attachment.invalid_signature",
                    "File content does not match the declared image format."));
            }

            var sanitizedFolder = string.IsNullOrWhiteSpace(folder) ? "uploads" : folder.Trim().ToLowerInvariant();
            return await fileStorage.UploadAsync(uploadStream, fileName, contentType, sanitizedFolder, ct);
        }
        finally
        {
            if (bufferedStream is not null)
                await bufferedStream.DisposeAsync();
        }
    }

    private static bool HasValidImageSignature(string contentType, byte[] header, int length)
    {
        if (contentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase))
            return length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;

        if (contentType.Equals("image/png", StringComparison.OrdinalIgnoreCase))
        {
            return length >= 8 &&
                   header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 &&
                   header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A;
        }

        if (contentType.Equals("image/webp", StringComparison.OrdinalIgnoreCase))
        {
            return length >= 12 &&
                   header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
                   header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50;
        }

        return false;
    }

    public Task<Result<string>> GetDownloadUrlAsync(string storageKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            return Task.FromResult(Result.Failure<string>(Error.Validation("attachment.invalid_key", "Storage key is required.")));

        return fileStorage.GetSignedUrlAsync(storageKey, TimeSpan.FromMinutes(15), ct);
    }
}
