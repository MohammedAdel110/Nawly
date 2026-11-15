using Nawly.Shared.EnumHelper.SupportEnums;

namespace Nawly.Shared.Common.Params.Support;

public class TicketQueryParams : BaseQueryParam
{
    public TicketStatus? Status { get; set; }
    public TicketCategory? Category { get; set; }
    public Guid? AssignedTo { get; set; }
}
