using Microsoft.Extensions.Configuration;
using Nawly.Abstraction.IServices.Storage;
using Nawly.Shared.Common.Results;
using Nawly.Shared.DTOs.Attachment;

namespace Nawly.Persistence.Adapters;

/// <summary>
/// Local filesystem implementation of <see cref="IFileStorage"/> for single-node/village deployment or local dev.
/// </summary>
public sealed class LocalDiskFileStorage(IConfiguration configuration) : IFileStorage
{
    private readonly string _rootDirectory = ResolveRootDirectory(configuration);

    public async Task<Result<UploadedFileDto>> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        string folder,
        CancellationToken ct = default)
    {
        var safeFolder = SanitizeSegment(folder, "uploads");
        var ext = Path.GetExtension(Path.GetFileName(fileName));
        if (string.IsNullOrWhiteSpace(ext) || ext.Length > 10)
            ext = ".bin";

        var uniqueName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var relativeKey = $"{safeFolder}/{uniqueName}";
        var targetDirectory = Path.Combine(_rootDirectory, safeFolder);
        Directory.CreateDirectory(targetDirectory);

        var fullPath = Path.Combine(targetDirectory, uniqueName);
        long sizeBytes;

        await using (var fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
        {
            await content.CopyToAsync(fs, ct);
            sizeBytes = fs.Length;
        }

        var publicBaseUrl = configuration["Storage:PublicBaseUrl"]?.TrimEnd('/');
        var publicUrl = string.IsNullOrWhiteSpace(publicBaseUrl)
            ? $"/uploads/{relativeKey}"
            : $"{publicBaseUrl}/{relativeKey}";

        return Result.Success(new UploadedFileDto(
            relativeKey,
            Path.GetFileName(fileName),
            contentType,
            sizeBytes,
            publicUrl));
    }

    public Task<Result> DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        var fullPath = ResolveSafePath(storageKey);
        if (fullPath is null)
            return Task.FromResult(Result.Failure(Error.Validation("storage.invalid_key", "Invalid storage key.")));

        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.FromResult(Result.Success());
    }

    public Task<Result<string>> GetSignedUrlAsync(
        string storageKey,
        TimeSpan expiresIn,
        CancellationToken ct = default)
    {
        var fullPath = ResolveSafePath(storageKey);
        if (fullPath is null)
            return Task.FromResult(Result.Failure<string>(Error.Validation("storage.invalid_key", "Invalid storage key.")));

        var normalizedKey = storageKey.Replace('\\', '/').TrimStart('/');
        var publicBaseUrl = configuration["Storage:PublicBaseUrl"]?.TrimEnd('/');
        var url = string.IsNullOrWhiteSpace(publicBaseUrl)
            ? $"/uploads/{normalizedKey}"
            : $"{publicBaseUrl}/{normalizedKey}";

        return Task.FromResult(Result.Success(url));
    }

    private string? ResolveSafePath(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            return null;

        var normalized = storageKey.Replace('\\', '/').TrimStart('/');
        var combined = Path.GetFullPath(Path.Combine(_rootDirectory, normalized));
        var rootFull = Path.GetFullPath(_rootDirectory);

        return combined.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase) ? combined : null;
    }

    private static string ResolveRootDirectory(IConfiguration configuration)
    {
        var configured = configuration["Storage:LocalRootPath"];
        if (!string.IsNullOrWhiteSpace(configured))
            return Path.GetFullPath(configured);

        return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads"));
    }

    private static string SanitizeSegment(string value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        var cleaned = new string(value.Trim().ToLowerInvariant()
            .Where(c => char.IsLetterOrDigit(c) || c is '-' or '_')
            .ToArray());

        return string.IsNullOrWhiteSpace(cleaned) ? fallback : cleaned;
    }
}
