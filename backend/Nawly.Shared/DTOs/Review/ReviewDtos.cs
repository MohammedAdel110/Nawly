using Nawly.Shared.EnumHelper.SupportEnums;

namespace Nawly.Shared.DTOs.Review;

public sealed record ReviewDto(
    Guid Id,
    Guid OrderId,
    Guid CustomerId,
    string CustomerFirstName,
    Guid StoreId,
    string StoreName,
    Guid? CourierId,
    int StoreRating,
    int? CourierRating,
    string? Comment,
    ReviewStatus Status,
    string? StoreReply,
    DateTimeOffset? StoreRepliedAt,
    DateTimeOffset CreatedAt);

public sealed record CreateReviewDto(int StoreRating, int? CourierRating, string? Comment);

public sealed record ReplyReviewDto(string Reply);

public sealed record ModerateReviewDto(ReviewStatus Status, string? Reason = null);
