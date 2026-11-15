using Nawly.Shared.EnumHelper.SupportEnums;

namespace Nawly.Shared.DTOs.Support;

public sealed record SupportMessageDto(
    Guid Id,
    Guid TicketId,
    Guid SenderUserId,
    string SenderName,
    string Body,
    string? AttachmentRef,
    DateTimeOffset CreatedAt);

public sealed record SupportTicketDto(
    Guid Id,
    Guid UserId,
    string UserName,
    Guid? OrderId,
    TicketCategory Category,
    string Subject,
    TicketStatus Status,
    Guid? AssignedTo,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    IReadOnlyList<SupportMessageDto> Messages);

public sealed record CreateSupportTicketDto(
    Guid? OrderId,
    TicketCategory Category,
    string Subject,
    string InitialMessage,
    string? AttachmentRef = null);

public sealed record ReplySupportTicketDto(
    string Body,
    string? AttachmentRef = null,
    TicketStatus? NewStatus = null);
