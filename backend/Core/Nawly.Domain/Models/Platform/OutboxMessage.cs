using Nawly.Domain.Models.Common;

namespace Nawly.Domain.Models.Platform;

public class OutboxMessage : BaseGuidEntity
{
    public string Type { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAt { get; set; }
    public int Attempts { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }
}
