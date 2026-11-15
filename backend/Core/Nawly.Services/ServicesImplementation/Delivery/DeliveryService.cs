using Nawly.Abstraction.IServices.Auth;
using Nawly.Abstraction.IServices.Delivery;
using Nawly.Abstraction.IServices.Dispatch;
using Nawly.Abstraction.IServices.Notification;
using Nawly.Domain.Contracts.SpecificReposPattern;
using Nawly.Domain.Contracts.UnitOfWorkPattern;
using Nawly.Domain.Events;
using Nawly.Domain.Models.Delivery;
using Nawly.Domain.Models.Identity;
using Nawly.Domain.ValueObjects;
using Nawly.Services.Mapping.DeliveryMapping;
using Nawly.Services.Specifications.CourierSpec;
using Nawly.Services.Specifications.DeliverySpec;
using Nawly.Services.Specifications.OrderSpec;
using Nawly.Services.Specifications.PromotionSpec;
using Nawly.Shared.Common.Results;
using Nawly.Shared.DTOs.Admin;
using Nawly.Shared.DTOs.Delivery;
using Nawly.Shared.EnumHelper.DeliveryEnums;
using Nawly.Shared.EnumHelper.OrderEnums;
using Nawly.Shared.EnumHelper.PaymentEnums;
using DeliveryEntity = Nawly.Domain.Models.Delivery.Delivery;

namespace Nawly.Services.ServicesImplementation.Delivery;

public sealed class DeliveryService(
    IUnitOfWork unitOfWork,
    IDeliveryRepository deliveryRepository,
    IOrderRepository orderRepository,
    IDispatchStrategy dispatchStrategy,
    IRealtimeNotifier realtimeNotifier,
    IDomainEventDispatcher domainEventDispatcher,
    IClock clock) : IDeliveryService
{
    private const decimal BaseCourierPayout = 12m;
    private const decimal PerKmCourierPayout = 3m;
    private static readonly TimeSpan OfferValidity = TimeSpan.FromSeconds(30);

    public async Task<Result<DeliveryDto>> CreateForOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        var existing = await deliveryRepository.GetByOrderIdAsync(orderId, ct);
        if (existing is not null)
            return Result.Success(existing.ToDto());

        var order = await orderRepository.GetByIdWithSpecAsync(new OrderWithDetailsSpecification(orderId, asNoTracking: true), ct);
        if (order is null)
            return Result.Failure<DeliveryDto>(Error.NotFound("delivery.order_not_found", "Order not found."));

        var distanceKm = 1.5;
        var storePt = order.Store.GetGeoPoint();
        if (storePt is not null && order.Latitude.HasValue && order.Longitude.HasValue)
            distanceKm = storePt.HaversineDistanceKm(new GeoPoint(order.Latitude.Value, order.Longitude.Value));

        var payout = decimal.Round(BaseCourierPayout + ((decimal)distanceKm * PerKmCourierPayout), 2, MidpointRounding.AwayFromZero);
        var codAmount = order.PaymentMethod == PaymentMethod.Cash ? order.Total : 0m;

        var delivery = new DeliveryEntity
        {
            OrderId = order.Id,
            DistanceKm = distanceKm,
            CourierPayout = payout,
            CodAmount = codAmount,
            CreatedAt = clock.UtcNow
        };

        await deliveryRepository.AddAsync(delivery, ct);
        await unitOfWork.SaveChangesAsync(ct);

        await DispatchWaveAsync(delivery.Id, 1, ct);

        var reloaded = await deliveryRepository.GetByIdWithSpecAsync(new DeliveryWithDetailsSpecification(delivery.Id, asNoTracking: true), ct);
        return Result.Success(reloaded!.ToDto());
    }

    public async Task<Result<int>> DispatchWaveAsync(Guid deliveryId, int wave, CancellationToken ct = default)
    {
        var delivery = await deliveryRepository.GetByIdWithSpecAsync(new DeliveryWithDetailsSpecification(deliveryId), ct);
        if (delivery is null)
            return Result.Failure<int>(Error.NotFound("delivery.not_found", "Delivery not found."));

        if (delivery.CourierId.HasValue || delivery.Status is not (DeliveryStatus.Unassigned or DeliveryStatus.Offered))
            return Result.Success(0);

        var store = delivery.Order.Store;
        if (!store.Latitude.HasValue || !store.Longitude.HasValue)
            return Result.Success(0);

        var freshLocationCutoff = clock.UtcNow.AddMinutes(-5);
        var eligibleCouriers = await deliveryRepository.GetEligibleCouriersForDispatchAsync(freshLocationCutoff, ct);

        var alreadyOffered = delivery.Offers.Select(o => o.CourierId).ToHashSet();
        var candidates = new List<DispatchCandidate>();

        foreach (var courier in eligibleCouriers)
        {
            if (alreadyOffered.Contains(courier.Id) || !courier.LastLat.HasValue || !courier.LastLng.HasValue)
                continue;

            var activeCount = (await deliveryRepository.GetActiveByCourierIdAsync(courier.Id, ct)).Count;
            candidates.Add(new DispatchCandidate(
                courier.Id,
                courier.LastLat.Value,
                courier.LastLng.Value,
                courier.RatingAvg,
                courier.LastDeliveredAt,
                activeCount,
                courier.MaxConcurrentDeliveries));
        }

        var selectedIds = dispatchStrategy.SelectCandidatesForWave(candidates, store.Latitude.Value, store.Longitude.Value, wave);
        if (selectedIds.Count == 0)
        {
            if (wave >= 3)
                await realtimeNotifier.NotifyDispatchAlertAsync(delivery.OrderId, "No eligible couriers available for delivery.", ct);

            return Result.Success(0);
        }

        var createdOffers = new List<DeliveryOffer>();
        foreach (var courierId in selectedIds)
        {
            var offer = delivery.CreateOffer(courierId, wave, clock.UtcNow, OfferValidity);
            offer.Delivery = delivery;
            createdOffers.Add(offer);
        }

        deliveryRepository.Update(delivery);
        await unitOfWork.SaveChangesAsync(ct);

        foreach (var offer in createdOffers)
            await realtimeNotifier.NotifyDeliveryOfferAsync(offer.CourierId, offer.ToDto(), ct);

        await DispatchAndClearAsync(delivery, ct);
        return Result.Success(createdOffers.Count);
    }

    public async Task<Result<IReadOnlyList<DeliveryOfferDto>>> GetPendingOffersForCourierAsync(
        Guid courierUserId,
        CancellationToken ct = default)
    {
        var courier = await unitOfWork
            .GetRepository<CourierProfile, Guid>()
            .GetByIdWithSpecAsync(new CourierByUserIdSpecification(courierUserId, asNoTracking: true), ct);

        if (courier is null)
            return Result.Failure<IReadOnlyList<DeliveryOfferDto>>(Error.NotFound("courier.not_found", "Courier profile not found."));

        var offers = await unitOfWork
            .GetRepository<DeliveryOffer, Guid>()
            .GetAllWithSpecAsync(new PendingOffersForCourierSpecification(courier.Id, clock.UtcNow), ct);

        return Result.Success<IReadOnlyList<DeliveryOfferDto>>(offers.Select(o => o.ToDto()).ToList());
    }

    public async Task<Result<DeliveryDto>> AcceptOfferAsync(Guid courierUserId, Guid offerId, CancellationToken ct = default)
    {
        var courier = await unitOfWork
            .GetRepository<CourierProfile, Guid>()
            .GetByIdWithSpecAsync(new CourierByUserIdSpecification(courierUserId), ct);

        if (courier is null || courier.Status != CourierStatus.Approved)
            return Result.Failure<DeliveryDto>(Error.Forbidden("courier.not_approved", "Courier is not approved."));

        var offer = await unitOfWork
            .GetRepository<DeliveryOffer, Guid>()
            .GetByIdWithSpecAsync(new DeliveryOfferWithDetailsSpecification(offerId, courier.Id), ct);

        if (offer is null)
            return Result.Failure<DeliveryDto>(Error.NotFound("delivery.offer_not_found", "Delivery offer not found."));

        if (!offer.IsStillValid(clock.UtcNow))
            return Result.Failure<DeliveryDto>(Error.Conflict("delivery.offer_expired", "This delivery offer has expired or been answered."));

        var claimed = await deliveryRepository.TryAssignCourierAtomicallyAsync(offer.DeliveryId, courier.Id, clock.UtcNow, ct);
        if (!claimed)
            return Result.Failure<DeliveryDto>(Error.Conflict("delivery.already_claimed", "Delivery has already been claimed by another courier."));

        var delivery = await deliveryRepository.GetByIdWithSpecAsync(new DeliveryWithDetailsSpecification(offer.DeliveryId), ct);
        if (delivery is null)
            return Result.Failure<DeliveryDto>(Error.NotFound("delivery.not_found", "Delivery not found."));

        var competingCouriers = delivery.Offers
            .Where(o => o.CourierId != courier.Id && o.Response == DeliveryOfferResponse.Pending)
            .Select(o => (o.CourierId, o.Id))
            .ToList();

        var assignResult = delivery.AssignCourier(courier.Id, clock.UtcNow);
        if (assignResult.IsFailure)
            return Result.Failure<DeliveryDto>(assignResult.Error);

        deliveryRepository.Update(delivery);
        await unitOfWork.SaveChangesAsync(ct);

        foreach (var (otherCourierId, otherOfferId) in competingCouriers)
            await realtimeNotifier.NotifyOfferExpiredOrTakenAsync(otherCourierId, otherOfferId, ct);

        await DispatchAndClearAsync(delivery, ct);
        return Result.Success(delivery.ToDto());
    }

    public async Task<Result> DeclineOfferAsync(Guid courierUserId, Guid offerId, CancellationToken ct = default)
    {
        var courier = await unitOfWork
            .GetRepository<CourierProfile, Guid>()
            .GetByIdWithSpecAsync(new CourierByUserIdSpecification(courierUserId, asNoTracking: true), ct);

        if (courier is null)
            return Result.Failure(Error.NotFound("courier.not_found", "Courier profile not found."));

        var offerRepo = unitOfWork.GetRepository<DeliveryOffer, Guid>();
        var offer = await offerRepo.GetByIdWithSpecAsync(new DeliveryOfferWithDetailsSpecification(offerId, courier.Id), ct);
        if (offer is null)
            return Result.Failure(Error.NotFound("delivery.offer_not_found", "Delivery offer not found."));

        if (offer.Response == DeliveryOfferResponse.Pending)
        {
            offer.Response = DeliveryOfferResponse.Declined;
            offer.RespondedAt = clock.UtcNow;
            offerRepo.Update(offer);
            await unitOfWork.SaveChangesAsync(ct);
        }

        return Result.Success();
    }

    public async Task<Result<DeliveryDto?>> GetActiveDeliveryForCourierAsync(Guid courierUserId, CancellationToken ct = default)
    {
        var courier = await unitOfWork
            .GetRepository<CourierProfile, Guid>()
            .GetByIdWithSpecAsync(new CourierByUserIdSpecification(courierUserId, asNoTracking: true), ct);

        if (courier is null)
            return Result.Failure<DeliveryDto?>(Error.NotFound("courier.not_found", "Courier profile not found."));

        var active = await deliveryRepository.GetByIdWithSpecAsync(new ActiveDeliveryForCourierSpecification(courier.Id), ct);
        return Result.Success<DeliveryDto?>(active?.ToDto());
    }

    public async Task<Result<DeliveryDto>> MarkArrivedAtStoreAsync(Guid courierUserId, Guid deliveryId, CancellationToken ct = default)
    {
        var (courier, delivery, error) = await LoadCourierDeliveryAsync(courierUserId, deliveryId, ct);
        if (error is not null)
            return Result.Failure<DeliveryDto>(error);

        var result = delivery!.MarkArrivedAtStore(courier!.Id, clock.UtcNow);
        if (result.IsFailure)
            return Result.Failure<DeliveryDto>(result.Error);

        deliveryRepository.Update(delivery);
        await unitOfWork.SaveChangesAsync(ct);
        await DispatchAndClearAsync(delivery, ct);
        return Result.Success(delivery.ToDto());
    }

    public async Task<Result<DeliveryDto>> MarkPickedUpAsync(Guid courierUserId, Guid deliveryId, CancellationToken ct = default)
    {
        var (courier, delivery, error) = await LoadCourierDeliveryAsync(courierUserId, deliveryId, ct);
        if (error is not null)
            return Result.Failure<DeliveryDto>(error);

        var result = delivery!.MarkPickedUp(courier!.Id, clock.UtcNow);
        if (result.IsFailure)
            return Result.Failure<DeliveryDto>(result.Error);

        var order = await orderRepository.GetByIdWithSpecAsync(new OrderWithDetailsSpecification(delivery.OrderId), ct);
        if (order is not null && order.Status == OrderStatus.ReadyForPickup)
        {
            var orderResult = order.MarkPickedUp(courierUserId, OrderActorRole.Courier, courier.Id, clock.UtcNow);
            if (orderResult.IsSuccess)
                orderRepository.Update(order);
        }

        deliveryRepository.Update(delivery);
        await unitOfWork.SaveChangesAsync(ct);

        await DispatchAndClearAsync(delivery, ct);
        if (order is not null && order.DomainEvents.Count > 0)
        {
            var events = order.DomainEvents.ToList();
            order.ClearDomainEvents();
            await domainEventDispatcher.DispatchAsync(events, ct);
        }

        return Result.Success(delivery.ToDto());
    }

    public async Task<Result<DeliveryDto>> MarkArrivedAtCustomerAsync(Guid courierUserId, Guid deliveryId, CancellationToken ct = default)
    {
        var (courier, delivery, error) = await LoadCourierDeliveryAsync(courierUserId, deliveryId, ct);
        if (error is not null)
            return Result.Failure<DeliveryDto>(error);

        var result = delivery!.MarkArrivedAtCustomer(courier!.Id, clock.UtcNow);
        if (result.IsFailure)
            return Result.Failure<DeliveryDto>(result.Error);

        deliveryRepository.Update(delivery);
        await unitOfWork.SaveChangesAsync(ct);
        await DispatchAndClearAsync(delivery, ct);
        return Result.Success(delivery.ToDto());
    }

    public async Task<Result<DeliveryDto>> MarkDeliveredAsync(
        Guid courierUserId,
        Guid deliveryId,
        ConfirmDeliveryCodeDto request,
        CancellationToken ct = default)
    {
        var (courier, delivery, error) = await LoadCourierDeliveryAsync(courierUserId, deliveryId, ct);
        if (error is not null)
            return Result.Failure<DeliveryDto>(error);

        var order = await orderRepository.GetByIdWithSpecAsync(new OrderWithDetailsSpecification(delivery!.OrderId), ct);
        if (order is null)
            return Result.Failure<DeliveryDto>(Error.NotFound("delivery.order_not_found", "Associated order not found."));

        if (!string.IsNullOrWhiteSpace(order.DeliveryConfirmationCodeHash) &&
            !ServiceHelpers.VerifySecretCode(request.Code, order.DeliveryConfirmationCodeHash))
        {
            return Result.Failure<DeliveryDto>(Error.Validation("delivery.invalid_pin", "Invalid 4-digit delivery confirmation code."));
        }

        var codCollected = delivery.CodAmount > 0m ? (request.CodCollectedAmount ?? delivery.CodAmount) : (decimal?)null;
        var deliveryResult = delivery.MarkDelivered(courier!.Id, codCollected, request.ProofPhotoRef, clock.UtcNow);
        if (deliveryResult.IsFailure)
            return Result.Failure<DeliveryDto>(deliveryResult.Error);

        var orderResult = order.MarkDelivered(courierUserId, OrderActorRole.Courier, courier.Id, clock.UtcNow);
        if (orderResult.IsFailure)
            return Result.Failure<DeliveryDto>(orderResult.Error);

        if (codCollected.HasValue && codCollected.Value > 0m)
            courier.AddCashOnHand(codCollected.Value);

        courier.LastDeliveredAt = clock.UtcNow;

        var customer = await unitOfWork.GetRepository<CustomerProfile, Guid>().GetByIdAsync(order.CustomerId, ct);
        customer?.RecordDeliveryCompleted();

        unitOfWork.GetRepository<CourierProfile, Guid>().Update(courier);
        deliveryRepository.Update(delivery);
        orderRepository.Update(order);
        await unitOfWork.SaveChangesAsync(ct);

        await DispatchAndClearAsync(delivery, ct);
        if (order.DomainEvents.Count > 0)
        {
            var events = order.DomainEvents.ToList();
            order.ClearDomainEvents();
            await domainEventDispatcher.DispatchAsync(events, ct);
        }

        return Result.Success(delivery.ToDto());
    }

    public async Task<Result<DeliveryDto>> MarkFailedAsync(
        Guid courierUserId,
        Guid deliveryId,
        FailDeliveryDto request,
        CancellationToken ct = default)
    {
        var (courier, delivery, error) = await LoadCourierDeliveryAsync(courierUserId, deliveryId, ct);
        if (error is not null)
            return Result.Failure<DeliveryDto>(error);

        var result = delivery!.MarkFailed(courier!.Id, request.ReasonCode, request.Note, null, clock.UtcNow);
        if (result.IsFailure)
            return Result.Failure<DeliveryDto>(result.Error);

        var order = await orderRepository.GetByIdWithSpecAsync(new OrderWithDetailsSpecification(delivery.OrderId), ct);
        if (order is not null && order.Status == OrderStatus.PickedUp)
        {
            var cancelCode = request.ReasonCode == DeliveryFailureReason.CustomerUnreachable
                ? CancelReasonCode.CustomerUnreachable
                : CancelReasonCode.CourierFault;
            order.MarkFailed(courierUserId, OrderActorRole.Courier, cancelCode, request.Note, clock.UtcNow);
            orderRepository.Update(order);

            if (request.ReasonCode == DeliveryFailureReason.CustomerUnreachable)
            {
                var customer = await unitOfWork.GetRepository<CustomerProfile, Guid>().GetByIdAsync(order.CustomerId, ct);
                customer?.RecordNoShow();
            }
        }

        deliveryRepository.Update(delivery);
        await unitOfWork.SaveChangesAsync(ct);

        await DispatchAndClearAsync(delivery, ct);
        return Result.Success(delivery.ToDto());
    }

    public async Task<Result<DeliveryDto>> AssignCourierManuallyAsync(
        Guid adminUserId,
        Guid deliveryId,
        AssignCourierRequestDto request,
        CancellationToken ct = default)
    {
        var delivery = await deliveryRepository.GetByIdWithSpecAsync(new DeliveryWithDetailsSpecification(deliveryId), ct);
        if (delivery is null)
            return Result.Failure<DeliveryDto>(Error.NotFound("delivery.not_found", "Delivery not found."));

        var courier = await unitOfWork.GetRepository<CourierProfile, Guid>().GetByIdAsync(request.CourierId, ct);
        if (courier is null || courier.Status != CourierStatus.Approved)
            return Result.Failure<DeliveryDto>(Error.NotFound("courier.not_found", "Approved courier not found."));

        if (delivery.CourierId.HasValue && delivery.CourierId.Value != courier.Id)
        {
            var unassign = delivery.UnassignForReassignment(clock.UtcNow);
            if (unassign.IsFailure)
                return Result.Failure<DeliveryDto>(unassign.Error);
        }

        var assign = delivery.AssignCourier(courier.Id, clock.UtcNow);
        if (assign.IsFailure)
            return Result.Failure<DeliveryDto>(assign.Error);

        delivery.UpdatedBy = adminUserId.ToString();
        deliveryRepository.Update(delivery);
        await unitOfWork.SaveChangesAsync(ct);

        await DispatchAndClearAsync(delivery, ct);
        return Result.Success(delivery.ToDto());
    }

    private async Task<(CourierProfile? Courier, DeliveryEntity? Delivery, Error? Error)> LoadCourierDeliveryAsync(
        Guid courierUserId,
        Guid deliveryId,
        CancellationToken ct)
    {
        var courier = await unitOfWork
            .GetRepository<CourierProfile, Guid>()
            .GetByIdWithSpecAsync(new CourierByUserIdSpecification(courierUserId), ct);

        if (courier is null)
            return (null, null, Error.NotFound("courier.not_found", "Courier profile not found."));

        var delivery = await deliveryRepository.GetByIdWithSpecAsync(new DeliveryWithDetailsSpecification(deliveryId), ct);
        if (delivery is null)
            return (courier, null, Error.NotFound("delivery.not_found", "Delivery not found."));

        return (courier, delivery, null);
    }

    private async Task DispatchAndClearAsync(DeliveryEntity delivery, CancellationToken ct)
    {
        if (delivery.DomainEvents.Count == 0)
            return;

        var events = delivery.DomainEvents.ToList();
        delivery.ClearDomainEvents();
        await domainEventDispatcher.DispatchAsync(events, ct);
    }
}
