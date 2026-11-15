using Nawly.Shared.Common.Pagination;
using Nawly.Shared.Common.Params.Review;
using Nawly.Shared.Common.Results;
using Nawly.Shared.DTOs.Review;

namespace Nawly.Abstraction.IServices.Review;

public interface IReviewService
{
    Task<Result<ReviewDto>> CreateReviewAsync(Guid customerUserId, Guid orderId, CreateReviewDto request, CancellationToken ct = default);
    Task<Result<PaginationResponse<ReviewDto>>> GetReviewsAsync(ReviewQueryParams queryParams, CancellationToken ct = default);
    Task<Result<ReviewDto>> ReplyToReviewAsync(Guid storeId, Guid reviewId, ReplyReviewDto request, CancellationToken ct = default);
    Task<Result<ReviewDto>> ModerateReviewAsync(Guid adminUserId, Guid reviewId, ModerateReviewDto request, CancellationToken ct = default);
}
