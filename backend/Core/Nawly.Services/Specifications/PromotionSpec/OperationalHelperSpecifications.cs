using Nawly.Domain.Contracts.SpecificationPattern.BaseSpec;
using Nawly.Domain.Models.Catalog;
using Nawly.Domain.Models.Delivery;
using Nawly.Domain.Models.Engagement;
using Nawly.Domain.Models.Finance;
using Nawly.Domain.Models.Promotions;
using Nawly.Shared.EnumHelper.FinanceEnums;

namespace Nawly.Services.Specifications.PromotionSpec;

public sealed class MenuCategoryByIdSpecification : BaseSpecifications<MenuCategory, Guid>
{
    public MenuCategoryByIdSpecification(Guid storeId, Guid categoryId)
        : base(c => c.StoreId == storeId && c.Id == categoryId && !c.IsDeleted)
    {
    }
}

public sealed class ProductOptionGroupByIdSpecification : BaseSpecifications<ProductOptionGroup, Guid>
{
    public ProductOptionGroupByIdSpecification(Guid productId, Guid groupId)
        : base(g => g.ProductId == productId && g.Id == groupId && !g.IsDeleted)
    {
        AddInclude(g => g.Options);
    }
}

public sealed class AllBannersSpecification : BaseSpecifications<Banner, Guid>
{
    public AllBannersSpecification()
        : base(b => !b.IsDeleted)
    {
        AddOrderBy(b => b.SortOrder);
        AddOrderBy(b => b.CreatedAt, isDescending: true);
        ApplyAsNoTracking();
    }
}

public sealed class ActiveCourierShiftSpecification : BaseSpecifications<CourierShift, Guid>
{
    public ActiveCourierShiftSpecification(Guid courierProfileId)
        : base(s => s.CourierId == courierProfileId && s.EndedAt == null && !s.IsDeleted)
    {
        AddOrderBy(s => s.StartedAt, isDescending: true);
    }
}

public sealed class DeliveryOfferWithDetailsSpecification : BaseSpecifications<DeliveryOffer, Guid>
{
    public DeliveryOfferWithDetailsSpecification(Guid offerId, Guid courierProfileId)
        : base(o => o.Id == offerId && o.CourierId == courierProfileId && !o.IsDeleted)
    {
        AddInclude("Delivery.Order.Store");
        AddInclude("Delivery.Offers");
    }
}

public sealed class LedgerEntriesForOrderSpecification : BaseSpecifications<LedgerEntry, Guid>
{
    public LedgerEntriesForOrderSpecification(Guid orderId)
        : base(e => e.OrderId == orderId && !e.IsDeleted)
    {
        ApplyAsNoTracking();
    }
}

public sealed class CourierCashLedgerSpecification : BaseSpecifications<LedgerEntry, Guid>
{
    public CourierCashLedgerSpecification(Guid? courierId = null)
        : base(e => !e.IsDeleted
            && e.AccountType == LedgerAccountType.Courier
            && e.Kind == LedgerEntryKind.CashCollected
            && (courierId == null || e.AccountId == courierId))
    {
        ApplyAsNoTracking();
    }
}
