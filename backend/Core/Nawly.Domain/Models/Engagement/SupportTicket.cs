using Nawly.Domain.Models.Common;
using Nawly.Domain.Models.Identity;
using Nawly.Shared.EnumHelper.SupportEnums;

namespace Nawly.Domain.Models.Engagement;

public class SupportTicket : BaseGuidEntity
{
    public Guid UserId { get; set; }
    public Guid? OrderId { get; set; }
    public TicketCategory Category { get; set; }
    public string Subject { get; set; } = string.Empty;
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public Guid? AssignedTo { get; set; }

    public AppUser User { get; set; } = null!;
    public ICollection<SupportMessage> Messages { get; set; } = [];
}
