using Nawly.Shared.Common.Pagination;
using Nawly.Shared.Common.Params.Promotion;
using Nawly.Shared.Common.Results;
using Nawly.Shared.DTOs.Promotion;

namespace Nawly.Abstraction.IServices.Promotion;

public interface IPromotionService
{
    Task<Result<CouponValidationDto>> ValidateCouponAsync(Guid userId, Guid storeId, decimal subtotal, decimal deliveryFee, string code, CancellationToken ct = default);
    Task<Result<PaginationResponse<CouponDto>>> GetCouponsAsync(CouponQueryParams queryParams, CancellationToken ct = default);
    Task<Result<CouponDto>> UpsertCouponAsync(Guid? couponId, UpsertCouponDto request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<BannerDto>>> GetActiveBannersAsync(CancellationToken ct = default);
    Task<Result<IReadOnlyList<BannerDto>>> GetAllBannersAsync(CancellationToken ct = default);
    Task<Result<BannerDto>> UpsertBannerAsync(Guid? bannerId, UpsertBannerDto request, CancellationToken ct = default);
    Task<Result> DeleteBannerAsync(Guid bannerId, CancellationToken ct = default);
}
