using System.Security.Cryptography;
using Nawly.Abstraction.IServices.Auth;
using Nawly.Abstraction.IServices.Delivery;
using Nawly.Abstraction.IServices.Notification;
using Nawly.Abstraction.IServices.Order;
using Nawly.Abstraction.IServices.Pricing;
using Nawly.Domain.Contracts.SpecificReposPattern;
using Nawly.Domain.Contracts.UnitOfWorkPattern;
using Nawly.Domain.Events;
using Nawly.Domain.Models.Identity;
using Nawly.Domain.Models.Ordering;
using Nawly.Domain.Models.Promotions;
using Nawly.Services.Mapping.OrderMapping;
using Nawly.Services.Specifications.DeliverySpec;
using Nawly.Services.Specifications.OrderSpec;
using Nawly.Services.Specifications.PromotionSpec;
using Nawly.Shared.Common.Pagination;
using Nawly.Shared.Common.Params.Order;
using Nawly.Shared.Common.Results;
using Nawly.Shared.DTOs.Order;
using Nawly.Shared.EnumHelper.OrderEnums;
using Nawly.Shared.EnumHelper.PaymentEnums;
using Nawly.Shared.EnumHelper.PromotionEnums;
using Nawly.Shared.EnumHelper.StoreEnums;
using AddressEntity = Nawly.Domain.Models.Geography.Address;
using CartEntity = Nawly.Domain.Models.Ordering.Cart;
using DeliveryEntity = Nawly.Domain.Models.Delivery.Delivery;
using OrderEntity = Nawly.Domain.Models.Ordering.Order;

namespace Nawly.Services.ServicesImplementation.Order;

public sealed class OrderService(
    IUnitOfWork unitOfWork,
    IOrderRepository orderRepository,
    IDeliveryService deliveryService,
    IPricingPolicy pricingPolicy,
    ICommissionPolicy commissionPolicy,
    IRealtimeNotifier realtimeNotifier,
    IDomainEventDispatcher domainEventDispatcher,
    IClock clock) : IOrderService
{
    private const decimal DefaultCommissionRate = 0.12m;
    private const decimal DefaultNewAccountCodLimit = 400m;

    public async Task<Result<OrderDetailsDto>> PlaceOrderAsync(
        Guid userId,
        PlaceOrderRequestDto request,
        Guid idempotencyKey,
        CancellationToken ct = default)
    {
        var effectiveIdempotencyKey = idempotencyKey != Guid.Empty
            ? idempotencyKey
            : request.IdempotencyKey ?? Guid.Empty;

        if (effectiveIdempotencyKey == Guid.Empty)
            return Result.Failure<OrderDetailsDto>(Error.Validation("order.idempotency_required", "Idempotency-Key header is required."));

        var customer = await ServiceHelpers.ResolveOrCreateCustomerProfileAsync(unitOfWork, userId, ct);
        var existingOrder = await orderRepository.GetByIdempotencyKeyAsync(customer.Id, effectiveIdempotencyKey, ct);
        if (existingOrder is not null)
            return Result.Success(existingOrder.ToDetailsDto(OrderActorRole.Customer));

        var cartRepo = unitOfWork.GetRepository<CartEntity, Guid>();
        var cart = await cartRepo.GetByIdWithSpecAsync(new ActiveCartByCustomerSpecification(customer.Id), ct);
        if (cart is null || cart.Items.Count == 0)
            return Result.Failure<OrderDetailsDto>(Error.Unprocessable("order.empty_cart", "Cannot place an order with an empty cart."));

        var store = cart.Store;
        if (!store.IsCurrentlyOpen(clock.CairoLocalTime, clock.CairoDayOfWeek, clock.UtcNow))
            return Result.Failure<OrderDetailsDto>(Error.Unprocessable("order.store_closed", "The store is currently closed or paused."));

        if (request.FulfilmentType == FulfilmentType.Pickup && store.DeliveryMode == DeliveryMode.PlatformCourier)
        {
            // Stores can still allow pickup unless explicitly disallowed; no hard block unless needed.
        }
        else if (request.FulfilmentType == FulfilmentType.Delivery && store.DeliveryMode == DeliveryMode.PickupOnly)
        {
            return Result.Failure<OrderDetailsDto>(Error.Unprocessable("order.pickup_only", "This store supports pickup only."));
        }

        AddressEntity? address = null;
        decimal zoneBaseFee = 0m;
        decimal? zonePerKmFee = null;
        double? distanceKm = null;
        decimal? freeThreshold = null;
        decimal minOrder = store.MinOrderAmount;
        var travelMinutes = 15;

        if (request.FulfilmentType == FulfilmentType.Delivery)
        {
            if (!request.AddressId.HasValue)
                return Result.Failure<OrderDetailsDto>(Error.Validation("order.address_required", "Delivery address is required."));

            address = await unitOfWork
                .GetRepository<AddressEntity, Guid>()
                .GetByIdWithSpecAsync(new AddressByIdSpecification(request.AddressId.Value, customer.Id, asNoTracking: true), ct);

            if (address is null || !address.Area.IsActive || !address.Area.Zone.IsActive)
                return Result.Failure<OrderDetailsDto>(Error.NotFound("order.address_not_found", "Selected delivery address or zone is unavailable."));

            var zone = address.Area.Zone;
            var storeZone = store.DeliveryZones.FirstOrDefault(dz => !dz.IsDeleted && dz.ZoneId == zone.Id);
            if (store.DeliveryZones.Count > 0 && storeZone is null)
                return Result.Failure<OrderDetailsDto>(Error.Unprocessable("order.address_out_of_range", "Store does not deliver to this area."));

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

        var orderItems = new List<OrderItem>();
        decimal subtotal = 0m;

        foreach (var cartItem in cart.Items.Where(i => !i.IsDeleted))
        {
            var product = cartItem.Product;
            if (product is null || !product.IsOrderable)
                return Result.Failure<OrderDetailsDto>(Error.Unprocessable("order.item_unavailable", $"{product?.Name ?? "Item"} is currently unavailable."));

            if (product.StockQuantity.HasValue && product.StockQuantity.Value < cartItem.Quantity)
                return Result.Failure<OrderDetailsDto>(Error.Unprocessable("order.insufficient_stock", $"Only {product.StockQuantity.Value} left of {product.Name}."));

            var selectedOptionSnapshots = cartItem.Options
                .Where(o => !o.IsDeleted && o.ProductOption is not null)
                .Select(o => new OrderItemOption
                {
                    GroupName = o.ProductOption.Group?.Name ?? string.Empty,
                    GroupNameAr = o.ProductOption.Group?.NameAr ?? string.Empty,
                    OptionName = o.ProductOption.Name,
                    OptionNameAr = o.ProductOption.NameAr,
                    PriceDelta = o.ProductOption.PriceDelta
                })
                .ToList();

            var unitPrice = product.Price + selectedOptionSnapshots.Sum(o => o.PriceDelta);
            var lineTotal = decimal.Round(unitPrice * cartItem.Quantity, 2, MidpointRounding.AwayFromZero);
            subtotal += lineTotal;

            product.DecrementStock(cartItem.Quantity);

            var orderItem = new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ProductNameAr = product.NameAr,
                UnitPrice = unitPrice,
                Quantity = cartItem.Quantity,
                LineTotal = lineTotal,
                Note = cartItem.Note,
                Options = selectedOptionSnapshots
            };
            orderItems.Add(orderItem);
        }

        subtotal = decimal.Round(subtotal, 2, MidpointRounding.AwayFromZero);
        if (subtotal < minOrder)
            return Result.Failure<OrderDetailsDto>(Error.Unprocessable("order.min_order_not_met", $"Minimum order for this store is {minOrder:0.00} EGP."));

        Coupon? coupon = null;
        decimal discountAmount = 0m;
        decimal storeFundedDiscount = 0m;

        if (!string.IsNullOrWhiteSpace(request.CouponCode))
        {
            var normalizedCode = request.CouponCode.Trim().ToUpperInvariant();
            coupon = await unitOfWork.GetRepository<Coupon, Guid>().GetByIdWithSpecAsync(new CouponByCodeSpecification(normalizedCode), ct);
            if (coupon is null)
                return Result.Failure<OrderDetailsDto>(Error.Unprocessable("order.invalid_coupon", "Coupon code not found."));

            var redemptionRepo = unitOfWork.GetRepository<CouponRedemption, Guid>();
            var totalRedemptions = await redemptionRepo.GetCountAsync(new CouponRedemptionsCountSpecification(coupon.Id), ct);
            var userRedemptions = await redemptionRepo.GetCountAsync(new CouponRedemptionsCountSpecification(coupon.Id, userId), ct);

            var preCouponDelivery = pricingPolicy.CalculateQuote(new PricingQuoteInput(
                subtotal, zoneBaseFee, zonePerKmFee, distanceKm, freeThreshold,
                request.FulfilmentType, request.PaymentMethod, 0m, false)).DeliveryFee;

            var couponCalc = coupon.CalculateDiscount(subtotal, preCouponDelivery, store.Id, totalRedemptions, userRedemptions, clock.UtcNow);
            if (couponCalc.IsFailure)
                return Result.Failure<OrderDetailsDto>(couponCalc.Error);

            discountAmount = couponCalc.Value;
            if (coupon.FundedBy == CouponFundingSource.Store && coupon.Type != CouponType.FreeDelivery)
                storeFundedDiscount = discountAmount;
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
            coupon?.Type == CouponType.FreeDelivery));

        if (request.PaymentMethod != PaymentMethod.Cash)
        {
            var onlineEnabled = await ServiceHelpers.GetBoolSettingAsync(unitOfWork, "Payments:OnlinePaymentsEnabled", false, ct);
            if (!onlineEnabled)
                return Result.Failure<OrderDetailsDto>(Error.Unprocessable("order.payment_method_disabled", "Only Cash payment is currently supported."));
        }
        else
        {
            if (customer.IsCodBlocked)
                return Result.Failure<OrderDetailsDto>(Error.Forbidden("order.cod_blocked", "Cash on Delivery is unavailable for this account."));

            var codCap = await ServiceHelpers.GetDecimalSettingAsync(unitOfWork, "Checkout:NewAccountCodLimitEgp", DefaultNewAccountCodLimit, ct);
            if (customer.IsNewAccountForCodCap() && quote.Total > codCap)
                return Result.Failure<OrderDetailsDto>(Error.Unprocessable("order.cod_limit_exceeded", $"New accounts can pay Cash on Delivery up to {codCap:0.00} EGP."));
        }

        var defaultCommissionRate = await ServiceHelpers.GetDecimalSettingAsync(unitOfWork, "Finance:DefaultCommissionRate", DefaultCommissionRate, ct);
        var effectiveRate = store.CommissionRate ?? defaultCommissionRate;
        var commissionAmount = commissionPolicy.CalculateCommission(quote.Subtotal, storeFundedDiscount, store.CommissionRate, defaultCommissionRate);

        var sequence = await orderRepository.GetNextDailySequenceAsync(clock.CairoDate, ct);
        var orderNumber = $"NAW-{clock.CairoDate:yyyyMMdd}-{sequence:D4}";

        var order = OrderEntity.CreateNew(
            orderNumber,
            customer.Id,
            store.Id,
            request.PaymentMethod,
            request.FulfilmentType,
            effectiveIdempotencyKey,
            clock.UtcNow);

        var user = await unitOfWork.GetRepository<AppUser, Guid>().GetByIdAsync(userId, ct);

        order.Subtotal = quote.Subtotal;
        order.DeliveryFee = quote.DeliveryFee;
        order.ServiceFee = quote.ServiceFee;
        order.TaxAmount = quote.TaxAmount;
        order.DiscountAmount = quote.DiscountAmount;
        order.CashRounding = quote.CashRounding;
        order.Total = quote.Total;
        order.CouponId = coupon?.Id;
        order.CommissionRate = effectiveRate;
        order.CommissionAmount = commissionAmount;
        order.CustomerNote = request.CustomerNote?.Trim();
        order.LeaveAtDoor = request.LeaveAtDoor;
        order.CustomerName = user?.FullName ?? "Customer";
        order.CustomerPhone = address?.ContactPhone ?? user?.PhoneNumber ?? string.Empty;

        if (request.FulfilmentType == FulfilmentType.Pickup)
        {
            order.AddressLabel = string.Empty;
            order.AreaName = string.Empty;
            order.AddressDescription = string.Empty;
            order.Building = null;
            order.Floor = null;
            order.Apartment = null;
            order.Latitude = null;
            order.Longitude = null;
            order.DeliveryNotes = null;
        }
        else
        {
            order.AddressLabel = address?.Label ?? string.Empty;
            order.AreaName = address?.Area?.Name ?? string.Empty;
            order.AddressDescription = address?.Description ?? string.Empty;
            order.Building = address?.Building;
            order.Floor = address?.Floor;
            order.Apartment = address?.Apartment;
            order.Latitude = address?.Latitude;
            order.Longitude = address?.Longitude;
            order.DeliveryNotes = address?.Notes;
        }

        order.EstimatedReadyAt = clock.UtcNow.AddMinutes(store.EffectivePrepMinutes);
        order.EstimatedDeliveryAt = clock.UtcNow.AddMinutes(store.EffectivePrepMinutes + travelMinutes);
        order.Store = store;

        if (request.FulfilmentType == FulfilmentType.Delivery)
        {
            var deliveryPin = RandomNumberGenerator.GetInt32(1000, 10000).ToString();
            order.DeliveryConfirmationCode = deliveryPin;
            order.DeliveryConfirmationCodeHash = ServiceHelpers.HashSecretCode(deliveryPin);
        }

        foreach (var item in orderItems)
            order.AddItem(item);

        await orderRepository.AddAsync(order, ct);

        if (request.PaymentMethod == PaymentMethod.Cash)
        {
            await unitOfWork.GetRepository<Domain.Models.Payments.Payment, Guid>().AddAsync(new Domain.Models.Payments.Payment
            {
                OrderId = order.Id,
                Method = PaymentMethod.Cash,
                Provider = PaymentProvider.Cash,
                Amount = order.Total,
                Currency = order.Currency,
                Status = PaymentStatus.Unpaid,
                IdempotencyKey = effectiveIdempotencyKey,
                InitiatedAt = clock.UtcNow,
                CreatedAt = clock.UtcNow
            }, ct);
        }

        if (coupon is not null)
        {
            await unitOfWork.GetRepository<CouponRedemption, Guid>().AddAsync(new CouponRedemption
            {
                CouponId = coupon.Id,
                UserId = userId,
                OrderId = order.Id,
                Amount = discountAmount,
                RedeemedAt = clock.UtcNow
            }, ct);
        }

        cart.Items.Clear();
        cart.IsDeleted = true;
        cart.DeletedAt = clock.UtcNow;
        cartRepo.Update(cart);

        await unitOfWork.SaveChangesAsync(ct);

        if (order.Status == OrderStatus.Placed)
            await realtimeNotifier.NotifyNewOrderAsync(store.Id, order.ToSummaryDto(), ct);

        await DispatchAndClearAsync(order, ct);
        return Result.Success(order.ToDetailsDto(OrderActorRole.Customer));
    }

    public async Task<Result<PaginationResponse<OrderSummaryDto>>> GetCustomerOrdersAsync(
        Guid customerId,
        OrderQueryParams queryParams,
        CancellationToken ct = default)
    {
        var customer = await ServiceHelpers.ResolveCustomerProfileAsync(unitOfWork, customerId, ct);
        var scopedParams = new OrderQueryParams
        {
            PageIndex = queryParams.PageIndex,
            PageSize = queryParams.PageSize,
            Search = queryParams.Search,
            Status = queryParams.Status,
            FromDate = queryParams.FromDate,
            ToDate = queryParams.ToDate,
            CustomerId = customer?.Id ?? customerId
        };

        return await SearchOrdersAsync(scopedParams, ct);
    }

    public Task<Result<PaginationResponse<OrderSummaryDto>>> GetStoreOrdersAsync(
        Guid storeId,
        OrderQueryParams queryParams,
        CancellationToken ct = default)
    {
        var scopedParams = new OrderQueryParams
        {
            PageIndex = queryParams.PageIndex,
            PageSize = queryParams.PageSize,
            Search = queryParams.Search,
            Status = queryParams.Status,
            FromDate = queryParams.FromDate,
            ToDate = queryParams.ToDate,
            StoreId = storeId
        };

        return SearchOrdersAsync(scopedParams, ct);
    }

    public async Task<Result<PaginationResponse<OrderSummaryDto>>> SearchOrdersAsync(
        OrderQueryParams queryParams,
        CancellationToken ct = default)
    {
        var items = await orderRepository.GetAllWithSpecAsync(new OrdersFilterSpecification(queryParams), ct);
        var total = await orderRepository.GetCountAsync(new OrdersCountSpecification(queryParams), ct);

        return Result.Success(new PaginationResponse<OrderSummaryDto>(
            queryParams.PageIndex,
            queryParams.PageSize,
            total,
            items.Select(o => o.ToSummaryDto()).ToList()));
    }

    public async Task<Result<OrderDetailsDto>> GetOrderDetailsAsync(
        Guid orderId,
        Guid actorUserId,
        IReadOnlyCollection<string> actorRoles,
        Guid? actorStoreId,
        CancellationToken ct = default)
    {
        var order = await orderRepository.GetByIdWithSpecAsync(new OrderWithDetailsSpecification(orderId, asNoTracking: true), ct);
        if (order is null)
            return Result.Failure<OrderDetailsDto>(Error.NotFound("order.not_found", "Order not found."));

        var isAdmin = actorRoles.Any(r => r.StartsWith("Admin", StringComparison.OrdinalIgnoreCase));
        var isStoreActor = actorStoreId.HasValue && actorStoreId.Value == order.StoreId;
        var customer = await ServiceHelpers.ResolveCustomerProfileAsync(unitOfWork, actorUserId, ct);
        var isOwner = customer is not null && customer.Id == order.CustomerId;

        DeliveryEntity? orderDelivery = null;
        if (order.FulfilmentType == FulfilmentType.Delivery)
        {
            var deliveries = await unitOfWork
                .GetRepository<DeliveryEntity, Guid>()
                .GetAllWithSpecAsync(new DeliveriesFilterSpecification(new Shared.Common.Params.Delivery.DeliveryQueryParams { Search = order.OrderNumber }), ct);
            orderDelivery = deliveries.FirstOrDefault(d => d.OrderId == order.Id);
        }

        var isCourier = orderDelivery?.Courier?.UserId == actorUserId;

        if (!isAdmin && !isStoreActor && !isOwner && !isCourier)
            return Result.Failure<OrderDetailsDto>(Error.Forbidden("order.forbidden", "You do not have access to this order."));

        var viewerRole = isAdmin
            ? OrderActorRole.Admin
            : isOwner
                ? OrderActorRole.Customer
                : isStoreActor
                    ? OrderActorRole.Store
                    : OrderActorRole.Courier;

        var courierFirstName = orderDelivery?.Courier?.User?.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        var rawCourierPhone = orderDelivery?.Courier?.User?.PhoneNumber;
        var courierPhone = string.IsNullOrWhiteSpace(rawCourierPhone) ? null : rawCourierPhone;

        return Result.Success(order.ToDetailsDto(viewerRole, courierFirstName, courierPhone));
    }

    public async Task<Result<OrderDetailsDto>> AcceptOrderAsync(
        Guid storeId,
        Guid orderId,
        Guid actorUserId,
        AcceptOrderDto request,
        CancellationToken ct = default)
    {
        var order = await orderRepository.GetByIdWithSpecAsync(new OrderWithDetailsSpecification(orderId), ct);
        if (order is null || order.StoreId != storeId)
            return Result.Failure<OrderDetailsDto>(Error.NotFound("order.not_found", "Order not found."));

        var travelMinutes = 15;
        var result = order.Accept(actorUserId, request.PrepMinutes, travelMinutes, clock.UtcNow);
        if (result.IsFailure)
            return Result.Failure<OrderDetailsDto>(result.Error);

        orderRepository.Update(order);
        await unitOfWork.SaveChangesAsync(ct);
        await DispatchAndClearAsync(order, ct);

        return Result.Success(order.ToDetailsDto(OrderActorRole.Store));
    }

    public async Task<Result<OrderDetailsDto>> RejectOrderAsync(
        Guid storeId,
        Guid orderId,
        Guid actorUserId,
        RejectOrderDto request,
        CancellationToken ct = default)
    {
        var order = await orderRepository.GetByIdWithSpecAsync(new OrderWithDetailsSpecification(orderId), ct);
        if (order is null || order.StoreId != storeId)
            return Result.Failure<OrderDetailsDto>(Error.NotFound("order.not_found", "Order not found."));

        var result = order.Reject(actorUserId, request.ReasonCode, request.ReasonText, clock.UtcNow);
        if (result.IsFailure)
            return Result.Failure<OrderDetailsDto>(result.Error);

        orderRepository.Update(order);
        await unitOfWork.SaveChangesAsync(ct);
        await DispatchAndClearAsync(order, ct);

        return Result.Success(order.ToDetailsDto(OrderActorRole.Store));
    }

    public async Task<Result<OrderDetailsDto>> MarkPreparingAsync(
        Guid storeId,
        Guid orderId,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        var order = await orderRepository.GetByIdWithSpecAsync(new OrderWithDetailsSpecification(orderId), ct);
        if (order is null || order.StoreId != storeId)
            return Result.Failure<OrderDetailsDto>(Error.NotFound("order.not_found", "Order not found."));

        var result = order.StartPreparing(actorUserId, clock.UtcNow);
        if (result.IsFailure)
            return Result.Failure<OrderDetailsDto>(result.Error);

        orderRepository.Update(order);
        await unitOfWork.SaveChangesAsync(ct);
        await DispatchAndClearAsync(order, ct);

        return Result.Success(order.ToDetailsDto(OrderActorRole.Store));
    }

    public async Task<Result<OrderDetailsDto>> MarkReadyForPickupAsync(
        Guid storeId,
        Guid orderId,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        var order = await orderRepository.GetByIdWithSpecAsync(new OrderWithDetailsSpecification(orderId), ct);
        if (order is null || order.StoreId != storeId)
            return Result.Failure<OrderDetailsDto>(Error.NotFound("order.not_found", "Order not found."));

        var result = order.MarkReadyForPickup(actorUserId, clock.UtcNow);
        if (result.IsFailure)
            return Result.Failure<OrderDetailsDto>(result.Error);

        orderRepository.Update(order);
        await unitOfWork.SaveChangesAsync(ct);
        await DispatchAndClearAsync(order, ct);

        if (order.FulfilmentType == FulfilmentType.Delivery && order.Store.DeliveryMode == DeliveryMode.PlatformCourier)
            await deliveryService.CreateForOrderAsync(order.Id, ct);

        return Result.Success(order.ToDetailsDto(OrderActorRole.Store));
    }

    public async Task<Result<OrderDetailsDto>> CancelByCustomerAsync(
        Guid customerId,
        Guid orderId,
        CancelOrderDto request,
        CancellationToken ct = default)
    {
        var customer = await ServiceHelpers.ResolveCustomerProfileAsync(unitOfWork, customerId, ct);
        var order = await orderRepository.GetByIdWithSpecAsync(new OrderWithDetailsSpecification(orderId), ct);

        if (order is null || customer is null || order.CustomerId != customer.Id)
            return Result.Failure<OrderDetailsDto>(Error.NotFound("order.not_found", "Order not found."));

        var result = order.Cancel(customerId, OrderActorRole.Customer, request.ReasonCode, request.ReasonText, clock.UtcNow);
        if (result.IsFailure)
            return Result.Failure<OrderDetailsDto>(result.Error);

        orderRepository.Update(order);
        await unitOfWork.SaveChangesAsync(ct);
        await DispatchAndClearAsync(order, ct);

        return Result.Success(order.ToDetailsDto(OrderActorRole.Customer));
    }

    public async Task<Result<OrderDetailsDto>> AdminChangeStatusAsync(
        Guid adminUserId,
        Guid orderId,
        AdminChangeOrderStatusDto request,
        CancellationToken ct = default)
    {
        var order = await orderRepository.GetByIdWithSpecAsync(new OrderWithDetailsSpecification(orderId), ct);
        if (order is null)
            return Result.Failure<OrderDetailsDto>(Error.NotFound("order.not_found", "Order not found."));

        var reason = request.ReasonCode ?? CancelReasonCode.AdminIntervention;
        Result transition = request.NewStatus switch
        {
            OrderStatus.Cancelled => order.Cancel(adminUserId, OrderActorRole.Admin, reason, request.ReasonText, clock.UtcNow),
            OrderStatus.Rejected => order.Reject(adminUserId, reason, request.ReasonText, clock.UtcNow, OrderActorRole.Admin),
            OrderStatus.Failed => order.MarkFailed(adminUserId, OrderActorRole.Admin, reason, request.ReasonText, clock.UtcNow),
            OrderStatus.Delivered => order.MarkDelivered(adminUserId, OrderActorRole.Admin, null, clock.UtcNow),
            _ => Result.Failure(Error.Validation("order.unsupported_admin_transition", $"Admin transition to '{request.NewStatus}' is not supported."))
        };

        if (transition.IsFailure)
            return Result.Failure<OrderDetailsDto>(transition.Error);

        orderRepository.Update(order);
        await unitOfWork.SaveChangesAsync(ct);
        await DispatchAndClearAsync(order, ct);

        return Result.Success(order.ToDetailsDto(OrderActorRole.Admin));
    }

    public async Task<int> AutoRejectExpiredOrdersAsync(TimeSpan timeout, CancellationToken ct = default)
    {
        var thresholdUtc = clock.UtcNow.Subtract(timeout);
        var expired = await orderRepository.GetExpiredUnacceptedOrdersAsync(thresholdUtc, ct);
        if (expired.Count == 0)
            return 0;

        var processed = 0;
        foreach (var order in expired)
        {
            var result = order.Cancel(null, OrderActorRole.System, CancelReasonCode.StoreTimeout, "Auto-cancelled due to store acceptance timeout.", clock.UtcNow);
            if (result.IsSuccess)
            {
                orderRepository.Update(order);
                processed++;
            }
        }

        if (processed > 0)
        {
            await unitOfWork.SaveChangesAsync(ct);
            foreach (var order in expired)
                await DispatchAndClearAsync(order, ct);
        }

        return processed;
    }

    private async Task DispatchAndClearAsync(OrderEntity order, CancellationToken ct)
    {
        if (order.DomainEvents.Count == 0)
            return;

        var events = order.DomainEvents.ToList();
        order.ClearDomainEvents();
        await domainEventDispatcher.DispatchAsync(events, ct);
    }
}
