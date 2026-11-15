using Nawly.Domain.Models.Common;
using Nawly.Domain.Models.Identity;

namespace Nawly.Domain.Models.Delivery;

public class CourierShift : BaseGuidEntity
{
    public Guid CourierId { get; set; }
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndedAt { get; set; }
    public string? EndReason { get; set; }

    public CourierProfile Courier { get; set; } = null!;

    public bool IsActive => !EndedAt.HasValue;

    public void End(string? reason, DateTimeOffset utcNow)
    {
        if (EndedAt.HasValue) return;
        EndedAt = utcNow;
        EndReason = reason;
    }
}
