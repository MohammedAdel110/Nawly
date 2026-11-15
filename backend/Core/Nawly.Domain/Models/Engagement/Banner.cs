using Nawly.Domain.Models.Common;
using Nawly.Shared.EnumHelper.PromotionEnums;

namespace Nawly.Domain.Models.Engagement;

public class Banner : BaseGuidEntity
{
    public string Title { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string ImageRef { get; set; } = string.Empty;
    public BannerTargetType TargetType { get; set; } = BannerTargetType.None;
    public Guid? TargetId { get; set; }
    public string? TargetUrl { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
