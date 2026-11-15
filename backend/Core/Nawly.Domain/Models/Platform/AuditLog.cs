using Nawly.Domain.Models.Common;

namespace Nawly.Domain.Models.Platform;

public class AuditLog : BaseGuidEntity
{
    public Guid? UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? CorrelationId { get; set; }
    public DateTimeOffset At { get; set; } = DateTimeOffset.UtcNow;
}
