using Nawly.Domain.Models.Catalog;
using Nawly.Domain.Models.Common;
using Nawly.Domain.Models.Identity;
using Nawly.Domain.Models.Ordering;
using Nawly.Shared.EnumHelper.SupportEnums;

namespace Nawly.Domain.Models.Engagement;

public class Review : BaseGuidEntity
{
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid StoreId { get; set; }
    public Guid? CourierId { get; set; }
    public int StoreRating { get; set; }
    public int? CourierRating { get; set; }
    public string? Comment { get; set; }
    public ReviewStatus Status { get; set; } = ReviewStatus.Visible;
    public string? StoreReply { get; set; }
    public DateTimeOffset? StoreRepliedAt { get; set; }

    public Order Order { get; set; } = null!;
    public CustomerProfile Customer { get; set; } = null!;
    public Store Store { get; set; } = null!;
    public CourierProfile? Courier { get; set; }
}
