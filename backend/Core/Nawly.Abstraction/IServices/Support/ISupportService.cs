using Nawly.Shared.Common.Pagination;
using Nawly.Shared.Common.Params.Support;
using Nawly.Shared.Common.Results;
using Nawly.Shared.DTOs.Support;

namespace Nawly.Abstraction.IServices.Support;

public interface ISupportService
{
    Task<Result<SupportTicketDto>> CreateTicketAsync(Guid userId, CreateSupportTicketDto request, CancellationToken ct = default);
    Task<Result<PaginationResponse<SupportTicketDto>>> GetMyTicketsAsync(Guid userId, TicketQueryParams queryParams, CancellationToken ct = default);
    Task<Result<PaginationResponse<SupportTicketDto>>> GetAllTicketsAsync(TicketQueryParams queryParams, CancellationToken ct = default);
    Task<Result<SupportTicketDto>> GetTicketDetailsAsync(Guid ticketId, Guid actorUserId, bool isAdmin, CancellationToken ct = default);
    Task<Result<SupportTicketDto>> ReplyToTicketAsync(Guid ticketId, Guid actorUserId, bool isAdmin, ReplySupportTicketDto request, CancellationToken ct = default);
}
