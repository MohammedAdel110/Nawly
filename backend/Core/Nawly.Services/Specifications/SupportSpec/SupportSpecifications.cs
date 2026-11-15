using Nawly.Domain.Contracts.SpecificationPattern.BaseSpec;
using Nawly.Domain.Models.Engagement;
using Nawly.Shared.Common.Params.Support;

namespace Nawly.Services.Specifications.SupportSpec;

public sealed class TicketsFilterSpecification : BaseSpecifications<SupportTicket, Guid>
{
    public TicketsFilterSpecification(TicketQueryParams queryParams, Guid? ownerUserId = null)
    {
        var status = queryParams.Status;
        var category = queryParams.Category;
        var assignedTo = queryParams.AssignedTo;
        var search = queryParams.Search;
        var hasSearch = !string.IsNullOrWhiteSpace(search);

        Criteria = t => !t.IsDeleted
            && (ownerUserId == null || t.UserId == ownerUserId)
            && (status == null || t.Status == status)
            && (category == null || t.Category == category)
            && (assignedTo == null || t.AssignedTo == assignedTo)
            && (!hasSearch || t.Subject.Contains(search!));

        AddInclude(t => t.User);
        AddInclude("Messages.Sender");
        AddOrderBy(t => t.CreatedAt, isDescending: true);
        ApplyPaging(queryParams.PageSize, queryParams.PageIndex);
        ApplyAsNoTracking();
        ApplySplitQuery();
    }
}

public sealed class TicketsCountSpecification : BaseSpecifications<SupportTicket, Guid>
{
    public TicketsCountSpecification(TicketQueryParams queryParams, Guid? ownerUserId = null)
    {
        var status = queryParams.Status;
        var category = queryParams.Category;
        var assignedTo = queryParams.AssignedTo;
        var search = queryParams.Search;
        var hasSearch = !string.IsNullOrWhiteSpace(search);

        Criteria = t => !t.IsDeleted
            && (ownerUserId == null || t.UserId == ownerUserId)
            && (status == null || t.Status == status)
            && (category == null || t.Category == category)
            && (assignedTo == null || t.AssignedTo == assignedTo)
            && (!hasSearch || t.Subject.Contains(search!));
    }
}

public sealed class TicketWithMessagesSpecification : BaseSpecifications<SupportTicket, Guid>
{
    public TicketWithMessagesSpecification(Guid ticketId, bool asNoTracking = false)
        : base(t => t.Id == ticketId && !t.IsDeleted)
    {
        AddInclude(t => t.User);
        AddInclude("Messages.Sender");
        ApplySplitQuery();
        if (asNoTracking)
            ApplyAsNoTracking();
    }
}
