using Nawly.Abstraction.IServices.Auth;
using Nawly.Abstraction.IServices.Support;
using Nawly.Domain.Contracts.UnitOfWorkPattern;
using Nawly.Domain.Models.Engagement;
using Nawly.Domain.Models.Identity;
using Nawly.Services.Mapping.SupportMapping;
using Nawly.Services.Specifications.SupportSpec;
using Nawly.Shared.Common.Pagination;
using Nawly.Shared.Common.Params.Support;
using Nawly.Shared.Common.Results;
using Nawly.Shared.DTOs.Support;
using Nawly.Shared.EnumHelper.SupportEnums;

namespace Nawly.Services.ServicesImplementation.Support;

public sealed class SupportService(
    IUnitOfWork unitOfWork,
    IClock clock) : ISupportService
{
    public async Task<Result<SupportTicketDto>> CreateTicketAsync(
        Guid userId,
        CreateSupportTicketDto request,
        CancellationToken ct = default)
    {
        var user = await unitOfWork.GetRepository<AppUser, Guid>().GetByIdAsync(userId, ct);
        if (user is null)
            return Result.Failure<SupportTicketDto>(Error.NotFound("support.user_not_found", "User not found."));

        var ticket = new SupportTicket
        {
            UserId = userId,
            User = user,
            OrderId = request.OrderId,
            Category = request.Category,
            Subject = request.Subject.Trim(),
            Status = TicketStatus.Open,
            CreatedAt = clock.UtcNow
        };

        ticket.Messages.Add(new SupportMessage
        {
            TicketId = ticket.Id,
            SenderUserId = userId,
            Sender = user,
            Body = request.InitialMessage.Trim(),
            AttachmentRef = request.AttachmentRef,
            CreatedAt = clock.UtcNow
        });

        await unitOfWork.GetRepository<SupportTicket, Guid>().AddAsync(ticket, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(ticket.ToDto());
    }

    public async Task<Result<PaginationResponse<SupportTicketDto>>> GetMyTicketsAsync(
        Guid userId,
        TicketQueryParams queryParams,
        CancellationToken ct = default)
    {
        var repo = unitOfWork.GetRepository<SupportTicket, Guid>();
        var items = await repo.GetAllWithSpecAsync(new TicketsFilterSpecification(queryParams, ownerUserId: userId), ct);
        var count = await repo.GetCountAsync(new TicketsCountSpecification(queryParams, ownerUserId: userId), ct);

        return Result.Success(new PaginationResponse<SupportTicketDto>(
            queryParams.PageIndex,
            queryParams.PageSize,
            count,
            items.Select(t => t.ToDto()).ToList()));
    }

    public async Task<Result<PaginationResponse<SupportTicketDto>>> GetAllTicketsAsync(
        TicketQueryParams queryParams,
        CancellationToken ct = default)
    {
        var repo = unitOfWork.GetRepository<SupportTicket, Guid>();
        var items = await repo.GetAllWithSpecAsync(new TicketsFilterSpecification(queryParams), ct);
        var count = await repo.GetCountAsync(new TicketsCountSpecification(queryParams), ct);

        return Result.Success(new PaginationResponse<SupportTicketDto>(
            queryParams.PageIndex,
            queryParams.PageSize,
            count,
            items.Select(t => t.ToDto()).ToList()));
    }

    public async Task<Result<SupportTicketDto>> GetTicketDetailsAsync(
        Guid ticketId,
        Guid actorUserId,
        bool isAdmin,
        CancellationToken ct = default)
    {
        var ticket = await unitOfWork
            .GetRepository<SupportTicket, Guid>()
            .GetByIdWithSpecAsync(new TicketWithMessagesSpecification(ticketId, asNoTracking: true), ct);

        if (ticket is null)
            return Result.Failure<SupportTicketDto>(Error.NotFound("support.ticket_not_found", "Support ticket not found."));

        if (!isAdmin && ticket.UserId != actorUserId)
            return Result.Failure<SupportTicketDto>(Error.Forbidden("support.forbidden", "You cannot access this ticket."));

        return Result.Success(ticket.ToDto());
    }

    public async Task<Result<SupportTicketDto>> ReplyToTicketAsync(
        Guid ticketId,
        Guid actorUserId,
        bool isAdmin,
        ReplySupportTicketDto request,
        CancellationToken ct = default)
    {
        var repo = unitOfWork.GetRepository<SupportTicket, Guid>();
        var ticket = await repo.GetByIdWithSpecAsync(new TicketWithMessagesSpecification(ticketId), ct);
        if (ticket is null)
            return Result.Failure<SupportTicketDto>(Error.NotFound("support.ticket_not_found", "Support ticket not found."));

        if (!isAdmin && ticket.UserId != actorUserId)
            return Result.Failure<SupportTicketDto>(Error.Forbidden("support.forbidden", "You cannot reply to this ticket."));

        var sender = await unitOfWork.GetRepository<AppUser, Guid>().GetByIdAsync(actorUserId, ct);
        if (sender is null)
            return Result.Failure<SupportTicketDto>(Error.NotFound("support.sender_not_found", "Sender user not found."));

        ticket.Messages.Add(new SupportMessage
        {
            TicketId = ticket.Id,
            SenderUserId = actorUserId,
            Sender = sender,
            Body = request.Body.Trim(),
            AttachmentRef = request.AttachmentRef,
            CreatedAt = clock.UtcNow
        });

        if (isAdmin)
        {
            ticket.AssignedTo ??= actorUserId;
            ticket.Status = request.NewStatus ?? TicketStatus.InProgress;
        }
        else if (ticket.Status == TicketStatus.Resolved)
        {
            ticket.Status = TicketStatus.Open;
        }

        ticket.UpdatedAt = clock.UtcNow;
        repo.Update(ticket);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(ticket.ToDto());
    }
}
