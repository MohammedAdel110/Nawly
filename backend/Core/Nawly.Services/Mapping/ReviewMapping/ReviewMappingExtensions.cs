using Nawly.Domain.Models.Engagement;
using Nawly.Shared.DTOs.Review;

namespace Nawly.Services.Mapping.ReviewMapping;

public static class ReviewMappingExtensions
{
    public static ReviewDto ToDto(this Review review)
    {
        var fullName = review.Customer?.User?.FullName ?? string.Empty;
        var firstName = string.IsNullOrWhiteSpace(fullName)
            ? string.Empty
            : fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];

        return new ReviewDto(
            review.Id,
            review.OrderId,
            review.CustomerId,
            firstName,
            review.StoreId,
            review.Store?.Name ?? string.Empty,
            review.CourierId,
            review.StoreRating,
            review.CourierRating,
            review.Comment,
            review.Status,
            review.StoreReply,
            review.StoreRepliedAt,
            review.CreatedAt);
    }
}
