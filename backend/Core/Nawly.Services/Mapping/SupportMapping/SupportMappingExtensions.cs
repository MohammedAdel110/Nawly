using Nawly.Domain.Models.Engagement;
using Nawly.Shared.DTOs.Support;

namespace Nawly.Services.Mapping.SupportMapping;

public static class SupportMappingExtensions
{
    public static SupportMessageDto ToDto(this SupportMessage message) => new(
        message.Id,
        message.TicketId,
        message.SenderUserId,
        message.Sender?.FullName ?? string.Empty,
        message.Body,
        message.AttachmentRef,
        message.CreatedAt);

    public static SupportTicketDto ToDto(this SupportTicket ticket) => new(
        ticket.Id,
        ticket.UserId,
        ticket.User?.FullName ?? string.Empty,
        ticket.OrderId,
        ticket.Category,
        ticket.Subject,
        ticket.Status,
        ticket.AssignedTo,
        ticket.CreatedAt,
        ticket.UpdatedAt,
        ticket.Messages
            .Where(m => !m.IsDeleted)
            .OrderBy(m => m.CreatedAt)
            .Select(m => m.ToDto())
            .ToList());
}
