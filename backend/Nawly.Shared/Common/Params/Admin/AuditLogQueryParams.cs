namespace Nawly.Shared.Common.Params.Admin;

public class AuditLogQueryParams : BaseQueryParam
{
    public Guid? UserId { get; set; }
    public string? EntityType { get; set; }
    public string? Action { get; set; }
    public DateTimeOffset? FromDate { get; set; }
    public DateTimeOffset? ToDate { get; set; }
}
