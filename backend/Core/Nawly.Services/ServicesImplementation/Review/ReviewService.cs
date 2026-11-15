using Nawly.Abstraction.IServices.Auth;
using Nawly.Abstraction.IServices.Review;
using Nawly.Domain.Contracts.SpecificReposPattern;
using Nawly.Domain.Contracts.UnitOfWorkPattern;
using Nawly.Domain.Models.Catalog;
using Nawly.Domain.Models.Engagement;
using Nawly.Domain.Models.Identity;
using Nawly.Services.Mapping.ReviewMapping;
using Nawly.Services.Specifications.OrderSpec;
using Nawly.Services.Specifications.ReviewSpec;
using Nawly.Shared.Common.Pagination;
using Nawly.Shared.Common.Params.Review;
using Nawly.Shared.Common.Results;
using Nawly.Shared.DTOs.Review;
using Nawly.Shared.EnumHelper.OrderEnums;
using StoreEntity = Nawly.Domain.Models.Catalog.Store;

namespace Nawly.Services.ServicesImplementation.Review;

public sealed class ReviewService(
    IUnitOfWork unitOfWork,
    IOrderRepository orderRepository,
    IDeliveryRepository deliveryRepository,
    IClock clock) : IReviewService
{
    public async Task<Result<ReviewDto>> CreateReviewAsync(
        Guid customerUserId,
        Guid orderId,
        CreateReviewDto request,
        CancellationToken ct = default)
    {
        var customer = await ServiceHelpers.ResolveCustomerProfileAsync(unitOfWork, customerUserId, ct);
        if (customer is null)
            return Result.Failure<ReviewDto>(Error.NotFound("review.order_not_found", "Order not found."));

        var order = await orderRepository.GetByIdWithSpecAsync(new OrderWithDetailsSpecification(orderId, asNoTracking: true), ct);
        if (order is null || order.CustomerId != customer.Id)
            return Result.Failure<ReviewDto>(Error.NotFound("review.order_not_found", "Order not found."));

        if (order.Status != OrderStatus.Delivered)
            return Result.Failure<ReviewDto>(Error.Unprocessable("review.order_not_delivered", "Only delivered orders can be reviewed."));

        var reviewRepo = unitOfWork.GetRepository<Domain.Models.Engagement.Review, Guid>();
        var existing = await reviewRepo.GetByIdWithSpecAsync(new ReviewByOrderIdSpecification(orderId), ct);
        if (existing is not null)
            return Result.Failure<ReviewDto>(Error.Conflict("review.already_submitted", "You have already reviewed this order."));

        var delivery = await deliveryRepository.GetByOrderIdAsync(orderId, ct);
        var courierId = delivery?.CourierId;

        var review = new Domain.Models.Engagement.Review
        {
            OrderId = order.Id,
            CustomerId = customer.Id,
            Customer = customer,
            StoreId = order.StoreId,
            Store = order.Store,
            CourierId = courierId,
            StoreRating = request.StoreRating,
            CourierRating = courierId.HasValue ? request.CourierRating : null,
            Comment = request.Comment?.Trim(),
            CreatedAt = clock.UtcNow
        };

        await reviewRepo.AddAsync(review, ct);

        var storeRepo = unitOfWork.GetRepository<StoreEntity, Guid>();
        var store = await storeRepo.GetByIdAsync(order.StoreId, ct);
        if (store is not null)
        {
            store.UpdateRatingAggregate(request.StoreRating);
            storeRepo.Update(store);
        }

        if (courierId.HasValue && request.CourierRating.HasValue)
        {
            var courierRepo = unitOfWork.GetRepository<CourierProfile, Guid>();
            var courier = await courierRepo.GetByIdAsync(courierId.Value, ct);
            if (courier is not null)
            {
                var total = (courier.RatingAvg * courier.RatingCount) + request.CourierRating.Value;
                courier.RatingCount++;
                courier.RatingAvg = decimal.Round(total / courier.RatingCount, 2, MidpointRounding.AwayFromZero);
                courierRepo.Update(courier);
            }
        }

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(review.ToDto());
    }

    public async Task<Result<PaginationResponse<ReviewDto>>> GetReviewsAsync(
        ReviewQueryParams queryParams,
        CancellationToken ct = default)
    {
        var repo = unitOfWork.GetRepository<Domain.Models.Engagement.Review, Guid>();
        var items = await repo.GetAllWithSpecAsync(new ReviewsFilterSpecification(queryParams), ct);
        var count = await repo.GetCountAsync(new ReviewsCountSpecification(queryParams), ct);

        return Result.Success(new PaginationResponse<ReviewDto>(
            queryParams.PageIndex,
            queryParams.PageSize,
            count,
            items.Select(r => r.ToDto()).ToList()));
    }

    public async Task<Result<ReviewDto>> ReplyToReviewAsync(
        Guid storeId,
        Guid reviewId,
        ReplyReviewDto request,
        CancellationToken ct = default)
    {
        var repo = unitOfWork.GetRepository<Domain.Models.Engagement.Review, Guid>();
        var review = await repo.GetByIdAsync(reviewId, ct);
        if (review is null || review.IsDeleted || review.StoreId != storeId)
            return Result.Failure<ReviewDto>(Error.NotFound("review.not_found", "Review not found."));

        review.StoreReply = request.Reply.Trim();
        review.StoreRepliedAt = clock.UtcNow;
        review.UpdatedAt = clock.UtcNow;
        repo.Update(review);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(review.ToDto());
    }

    public async Task<Result<ReviewDto>> ModerateReviewAsync(
        Guid adminUserId,
        Guid reviewId,
        ModerateReviewDto request,
        CancellationToken ct = default)
    {
        var repo = unitOfWork.GetRepository<Domain.Models.Engagement.Review, Guid>();
        var review = await repo.GetByIdAsync(reviewId, ct);
        if (review is null || review.IsDeleted)
            return Result.Failure<ReviewDto>(Error.NotFound("review.not_found", "Review not found."));

        review.Status = request.Status;
        review.UpdatedAt = clock.UtcNow;
        review.UpdatedBy = adminUserId.ToString();
        repo.Update(review);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(review.ToDto());
    }
}
