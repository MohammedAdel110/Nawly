using Nawly.Abstraction.IServices.Auth;
using Nawly.Abstraction.IServices.Cart;
using Nawly.Abstraction.IServices.Pricing;
using Nawly.Abstraction.IServices.Promotion;
using Nawly.Domain.Contracts.UnitOfWorkPattern;
using Nawly.Domain.Models.Catalog;
using Nawly.Domain.ValueObjects;
using Nawly.Services.Mapping.CartMapping;
using Nawly.Services.Specifications.CatalogSpec;
using Nawly.Services.Specifications.OrderSpec;
using Nawly.Shared.Common.Results;
using Nawly.Shared.DTOs.Cart;
using Nawly.Shared.EnumHelper.OrderEnums;
using Nawly.Shared.EnumHelper.PaymentEnums;
using AddressEntity = Nawly.Domain.Models.Geography.Address;
using CartEntity = Nawly.Domain.Models.Ordering.Cart;

namespace Nawly.Services.ServicesImplementation.Cart;

public sealed class CartService(
    IUnitOfWork unitOfWork,
    IPricingPolicy pricingPolicy,
    IPromotionService promotionService,
    IClock clock) : ICartService
{
    private const decimal DefaultNewAccountCodLimit = 400m;

    public async Task<Result<CartDto?>> GetActiveCartAsync(Guid userId, CancellationToken ct = default)
    {
        var customer = await ServiceHelpers.ResolveCustomerProfileAsync(unitOfWork, userId, ct);
        if (customer is null)
            return Result.Success<CartDto?>(null);

        var cart = await unitOfWork
            .GetRepository<CartEntity, Guid>()
            .GetByIdWithSpecAsync(new ActiveCartByCustomerSpecification(customer.Id, asNoTracking: true), ct);

        return Result.Success<CartDto?>(cart?.ToDto());
    }

    public async Task<Result<CartDto>> UpsertItemAsync(Guid userId, UpsertCartItemDto request, CancellationToken ct = default)
    {
        var product = await unitOfWork
            .GetRepository<Product, Guid>()
            .GetByIdWithSpecAsync(new ProductWithOptionsSpecification(request.ProductId, asNoTracking: true), ct);

        if (product is null || !product.IsOrderable)
            return Result.Failure<CartDto>(Error.NotFound("cart.product_unavailable", "Product is not available."));

        var validOptionIds = product.OptionGroups
            .Where(g => !g.IsDeleted)
            .SelectMany(g => g.Options)
            .Where(o => !o.IsDeleted && o.IsAvailable)
            .Select(o => o.Id)
            .ToHashSet();

        if (request.SelectedOptionIds.Any(id => !validOptionIds.Contains(id)))
            return Result.Failure<CartDto>(Error.Validation("cart.invalid_option", "One or more selected options are unavailable."));

        var customer = await ServiceHelpers.ResolveOrCreateCustomerProfileAsync(unitOfWork, userId, ct);
        var cartRepo = unitOfWork.GetRepository<CartEntity, Guid>();
        var cart = await cartRepo.GetByIdWithSpecAsync(new ActiveCartByCustomerSpecification(customer.Id), ct);

        if (cart is null)
        {
            cart = new CartEntity
            {
                CustomerId = customer.Id,
                StoreId = product.StoreId,
                CreatedAt = clock.UtcNow
            };

            var addResult = cart.UpsertItem(
                product.Id,
                request.Quantity,
                request.Note?.Trim(),
                request.SelectedOptionIds,
                clock.UtcNow,
                request.ReplaceCartIfDifferentStore,
                product.StoreId);

            if (addResult.IsFailure)
                return Result.Failure<CartDto>(addResult.Error);

            await cartRepo.AddAsync(cart, ct);
        }
        else
        {
            var upsertResult = cart.UpsertItem(
                product.Id,
                request.Quantity,
                request.Note?.Trim(),
                request.SelectedOptionIds,
                clock.UtcNow,
                request.ReplaceCartIfDifferentStore,
                product.StoreId);

            if (upsertResult.IsFailure)
                return Result.Failure<CartDto>(upsertResult.Error);

            cartRepo.Update(cart);
        }

        await unitOfWork.SaveChangesAsync(ct);

        var reloaded = await cartRepo.GetByIdWithSpecAsync(new ActiveCartByCustomerSpecification(customer.Id, asNoTracking: true), ct);
        return Result.Success(reloaded!.ToDto());
    }

    public async Task<Result<CartDto>> RemoveItemAsync(Guid userId, Guid cartItemId, CancellationToken ct = default)
    {
        var customer = await ServiceHelpers.ResolveCustomerProfileAsync(unitOfWork, userId, ct);
        if (customer is null)
            return Result.Failure<CartDto>(Error.NotFound("cart.not_found", "Active cart not found."));

        var cartRepo = unitOfWork.GetRepository<CartEntity, Guid>();
        var cart = await cartRepo.GetByIdWithSpecAsync(new ActiveCartByCustomerSpecification(customer.Id), ct);
        if (cart is null)
            return Result.Failure<CartDto>(Error.NotFound("cart.not_found", "Active cart not found."));

        var removeResult = cart.RemoveItem(cartItemId, clock.UtcNow);
        if (removeResult.IsFailure)
            return Result.Failure<CartDto>(removeResult.Error);

        cartRepo.Update(cart);
        await unitOfWork.SaveChangesAsync(ct);

        var reloaded = await cartRepo.GetByIdWithSpecAsync(new ActiveCartByCustomerSpecification(customer.Id, asNoTracking: true), ct);
        return Result.Success(reloaded!.ToDto());
    }

    public async Task<Result> ClearCartAsync(Guid userId, CancellationToken ct = default)
    {
        var customer = await ServiceHelpers.ResolveCustomerProfileAsync(unitOfWork, userId, ct);
        if (customer is null)
            return Result.Success();

        var cartRepo = unitOfWork.GetRepository<CartEntity, Guid>();
        var cart = await cartRepo.GetByIdWithSpecAsync(new ActiveCartByCustomerSpecification(customer.Id), ct);
        if (cart is null)
            return Result.Success();

        cart.Items.Clear();
        cart.IsDeleted = true;
        cart.DeletedAt = clock.UtcNow;
        cartRepo.Update(cart);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<CartQuoteResponseDto>> QuoteCartAsync(
        Guid userId,
        CartQuoteRequestDto request,
        CancellationToken ct = default)
    {
        var customer = await ServiceHelpers.ResolveCustomerProfileAsync(unitOfWork, userId, ct);
        if (customer is null)
            return Result.Failure<CartQuoteResponseDto>(Error.NotFound("cart.empty", "Cart is empty."));

        var cart = await unitOfWork
            .GetRepository<CartEntity, Guid>()
            .GetByIdWithSpecAsync(new ActiveCartByCustomerSpecification(customer.Id, asNoTracking: true), ct);

        if (cart is null || cart.Items.Count == 0)
            return Result.Failure<CartQuoteResponseDto>(Error.NotFound("cart.empty", "Cart is empty."));

        var violations = new List<QuoteViolationDto>();
        var store = cart.Store;

        if (!store.IsCurrentlyOpen(clock.CairoLocalTime, clock.CairoDayOfWeek, clock.UtcNow))
            violations.Add(new QuoteViolationDto("store.closed", "This store is currently closed or paused."));

        foreach (var item in cart.Items.Where(i => !i.IsDeleted))
        {
            if (item.Product is null || !item.Product.IsOrderable)
                violations.Add(new QuoteViolationDto("cart.item_unavailable", $"{item.Product?.Name ?? "Item"} is unavailable.", item.ProductId));
        }

        var mapped = cart.ToDto();
        var subtotal = mapped.ItemsSubtotal;

        var zoneBaseFee = 0m;
        decimal? zonePerKmFee = null;
        double? distanceKm = null;
        decimal? freeThreshold = null;
        var travelMinutes = 0;
        var minOrder = store.MinOrderAmount;

        if (request.FulfilmentType == FulfilmentType.Delivery)
        {
            if (!request.AddressId.HasValue)
            {
                violations.Add(new QuoteViolationDto("address.required", "Delivery address is required."));
            }
            else
            {
                var address = await unitOfWork
                    .GetRepository<AddressEntity, Guid>()
                    .GetByIdWithSpecAsync(new AddressByIdSpecification(request.AddressId.Value, customer.Id, asNoTracking: true), ct);

                if (address is null || !address.Area.IsActive || !address.Area.Zone.IsActive)
                {
                    violations.Add(new QuoteViolationDto("address.invalid", "Selected delivery address or zone is unavailable."));
                }
                else
                {
                    var zone = address.Area.Zone;
                    var storeZone = store.DeliveryZones.FirstOrDefault(dz => !dz.IsDeleted && dz.ZoneId == zone.Id);
                    if (store.DeliveryZones.Count > 0 && storeZone is null)
                        violations.Add(new QuoteViolationDto("address.out_of_range", "Store does not deliver to this area."));

                    zoneBaseFee = storeZone?.FeeOverride ?? zone.BaseFee;
                    zonePerKmFee = zone.PerKmFee;
                    freeThreshold = zone.FreeDeliveryThreshold;
                    minOrder = Math.Max(store.MinOrderAmount, storeZone?.MinOrderOverride ?? zone.MinOrderAmount);
                    travelMinutes = zone.EstimatedTravelMinutes;

                    var storePt = store.GetGeoPoint();
                    var addrPt = address.GetGeoPoint();
                    if (storePt is not null && addrPt is not null)
                        distanceKm = storePt.HaversineDistanceKm(addrPt);
                }
            }
        }

        if (subtotal < minOrder)
            violations.Add(new QuoteViolationDto("cart.min_order", $"Minimum order is {minOrder:0.00} EGP."));

        decimal discountAmount = 0m;
        var couponApplied = false;
        string? appliedCode = null;
        var isFreeDeliveryCoupon = false;

        if (!string.IsNullOrWhiteSpace(request.CouponCode))
        {
            var preCouponDeliveryFee = pricingPolicy.CalculateQuote(new PricingQuoteInput(
                subtotal, zoneBaseFee, zonePerKmFee, distanceKm, freeThreshold,
                request.FulfilmentType, request.PaymentMethod, 0m, false)).DeliveryFee;

            var couponResult = await promotionService.ValidateCouponAsync(
                userId, store.Id, subtotal, preCouponDeliveryFee, request.CouponCode, ct);

            if (couponResult.IsSuccess && couponResult.Value.IsValid)
            {
                discountAmount = couponResult.Value.DiscountAmount;
                couponApplied = true;
                appliedCode = couponResult.Value.Code;
                isFreeDeliveryCoupon = preCouponDeliveryFee > 0m && discountAmount == preCouponDeliveryFee;
            }
            else
            {
                violations.Add(new QuoteViolationDto("coupon.invalid", couponResult.IsSuccess ? couponResult.Value.FailureReason ?? "Invalid coupon." : couponResult.Error.Message));
            }
        }

        var quote = pricingPolicy.CalculateQuote(new PricingQuoteInput(
            subtotal,
            zoneBaseFee,
            zonePerKmFee,
            distanceKm,
            freeThreshold,
            request.FulfilmentType,
            request.PaymentMethod,
            discountAmount,
            isFreeDeliveryCoupon));

        if (request.PaymentMethod != PaymentMethod.Cash)
        {
            var onlineEnabled = await ServiceHelpers.GetBoolSettingAsync(unitOfWork, "Payments:OnlinePaymentsEnabled", false, ct);
            if (!onlineEnabled)
                violations.Add(new QuoteViolationDto("checkout.payment_method_disabled", "Only Cash payment is currently supported."));
        }
        else
        {
            if (customer.IsCodBlocked)
                violations.Add(new QuoteViolationDto("checkout.cod_blocked", "Cash on Delivery is unavailable for this account."));

            var codCap = await ServiceHelpers.GetDecimalSettingAsync(unitOfWork, "Checkout:NewAccountCodLimitEgp", DefaultNewAccountCodLimit, ct);
            if (customer.IsNewAccountForCodCap() && quote.Total > codCap)
                violations.Add(new QuoteViolationDto("checkout.cod_limit_exceeded", $"New accounts are capped at {codCap:0.00} EGP for Cash on Delivery."));
        }

        var response = new CartQuoteResponseDto(
            violations.Count == 0,
            quote.Subtotal,
            quote.DeliveryFee,
            quote.ServiceFee,
            quote.TaxAmount,
            quote.DiscountAmount,
            quote.CashRounding,
            quote.Total,
            quote.Currency,
            store.EffectivePrepMinutes + travelMinutes,
            couponApplied,
            appliedCode,
            violations);

        return Result.Success(response);
    }
}
