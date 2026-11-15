using Nawly.Domain.Models.Common;

namespace Nawly.Domain.Models.Platform;

public class IdempotencyRecord : BaseGuidEntity
{
    public Guid Key { get; set; }
    public Guid UserId { get; set; }
    public string RequestHash { get; set; } = string.Empty;
    public int ResponseStatus { get; set; }
    public string ResponseBody { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
}
