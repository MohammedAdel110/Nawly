using Nawly.Domain.Contracts.SpecificationPattern.BaseSpec;
using Nawly.Domain.Models.Engagement;
using Nawly.Shared.Common.Params.Review;

namespace Nawly.Services.Specifications.ReviewSpec;

public sealed class ReviewsFilterSpecification : BaseSpecifications<Review, Guid>
{
    public ReviewsFilterSpecification(ReviewQueryParams queryParams)
    {
        var storeId = queryParams.StoreId;
        var status = queryParams.Status;
        var rating = queryParams.Rating;

        Criteria = r => !r.IsDeleted
            && (storeId == null || r.StoreId == storeId)
            && (status == null || r.Status == status)
            && (rating == null || r.StoreRating == rating);

        AddInclude(r => r.Store);
        AddInclude("Customer.User");
        AddOrderBy(r => r.CreatedAt, isDescending: true);
        ApplyPaging(queryParams.PageSize, queryParams.PageIndex);
        ApplyAsNoTracking();
        ApplySplitQuery();
    }
}

public sealed class ReviewsCountSpecification : BaseSpecifications<Review, Guid>
{
    public ReviewsCountSpecification(ReviewQueryParams queryParams)
    {
        var storeId = queryParams.StoreId;
        var status = queryParams.Status;
        var rating = queryParams.Rating;

        Criteria = r => !r.IsDeleted
            && (storeId == null || r.StoreId == storeId)
            && (status == null || r.Status == status)
            && (rating == null || r.StoreRating == rating);
    }
}

public sealed class ReviewByOrderIdSpecification : BaseSpecifications<Review, Guid>
{
    public ReviewByOrderIdSpecification(Guid orderId)
        : base(r => !r.IsDeleted && r.OrderId == orderId)
    {
        AddInclude(r => r.Store);
        AddInclude("Customer.User");
    }
}
