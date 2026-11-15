namespace Nawly.Shared.DTOs.Attachment;

public sealed record UploadedFileDto(
    string StorageKey,
    string FileName,
    string ContentType,
    long SizeBytes,
    string? PublicUrl = null);
