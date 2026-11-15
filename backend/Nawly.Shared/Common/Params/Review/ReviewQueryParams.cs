using Nawly.Shared.EnumHelper.SupportEnums;

namespace Nawly.Shared.Common.Params.Review;

public class ReviewQueryParams : BaseQueryParam
{
    public Guid? StoreId { get; set; }
    public ReviewStatus? Status { get; set; }
    public int? Rating { get; set; }
}
