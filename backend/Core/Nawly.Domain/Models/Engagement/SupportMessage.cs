using Nawly.Domain.Models.Common;
using Nawly.Domain.Models.Identity;

namespace Nawly.Domain.Models.Engagement;

public class SupportMessage : BaseGuidEntity
{
    public Guid TicketId { get; set; }
    public Guid SenderUserId { get; set; }
    public string Body { get; set; } = string.Empty;
    public string? AttachmentRef { get; set; }

    public SupportTicket Ticket { get; set; } = null!;
    public AppUser Sender { get; set; } = null!;
}
