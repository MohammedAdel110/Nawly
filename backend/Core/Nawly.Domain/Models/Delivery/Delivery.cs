using Nawly.Domain.Events;
using Nawly.Domain.Models.Common;
using Nawly.Domain.Models.Identity;
using Nawly.Domain.Models.Ordering;
using Nawly.Shared.Common.Results;
using Nawly.Shared.EnumHelper.DeliveryEnums;

namespace Nawly.Domain.Models.Delivery;

public class Delivery : AggregateRoot
{
    public Guid OrderId { get; set; }
    public Guid? CourierId { get; private set; }
    public DeliveryStatus Status { get; private set; } = DeliveryStatus.Unassigned;
    public double DistanceKm { get; set; }
    public decimal CourierPayout { get; set; }
    public decimal CodAmount { get; set; }
    public decimal? CodCollectedAmount { get; private set; }
    public DateTimeOffset? AssignedAt { get; private set; }
    public DateTimeOffset? ArrivedStoreAt { get; private set; }
    public DateTimeOffset? PickedUpAt { get; private set; }
    public DateTimeOffset? ArrivedCustomerAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public DateTimeOffset? FailedAt { get; private set; }
    public DeliveryFailureReason? FailureReasonCode { get; private set; }
    public string? FailureNote { get; private set; }
    public string? ProofPhotoRef { get; set; }
    public string? PickupCodeHash { get; set; }
    public uint RowVersion { get; set; }

    public Order Order { get; set; } = null!;
    public CourierProfile? Courier { get; set; }
    public ICollection<DeliveryOffer> Offers { get; set; } = [];

    public DeliveryOffer CreateOffer(Guid courierId, int wave, DateTimeOffset utcNow, TimeSpan validity)
    {
        if (Status == DeliveryStatus.Unassigned)
            Status = DeliveryStatus.Offered;

        var offer = new DeliveryOffer
        {
            DeliveryId = Id,
            CourierId = courierId,
            Wave = wave,
            OfferedAt = utcNow,
            ExpiresAt = utcNow.Add(validity),
            Response = DeliveryOfferResponse.Pending
        };

        Offers.Add(offer);
        Raise(new DeliveryOfferedEvent(Id, OrderId, courierId, wave, offer.ExpiresAt));
        return offer;
    }

    public Result AssignCourier(Guid courierId, DateTimeOffset utcNow)
    {
        if (Status is not (DeliveryStatus.Unassigned or DeliveryStatus.Offered))
            return InvalidTransition(DeliveryStatus.Assigned);

        var previous = Status;
        CourierId = courierId;
        Status = DeliveryStatus.Assigned;
        AssignedAt = utcNow;
        UpdatedAt = utcNow;

        foreach (var offer in Offers.Where(o => o.Response == DeliveryOfferResponse.Pending))
        {
            offer.Response = offer.CourierId == courierId
                ? DeliveryOfferResponse.Accepted
                : DeliveryOfferResponse.Cancelled;
            offer.RespondedAt = utcNow;
        }

        Raise(new DeliveryAssignedEvent(Id, OrderId, courierId));
        Raise(new DeliveryStatusChangedEvent(Id, OrderId, courierId, previous, Status));
        return Result.Success();
    }

    public Result MarkArrivedAtStore(Guid courierId, DateTimeOffset utcNow)
    {
        if (CourierId != courierId)
            return Result.Failure(Error.Forbidden("delivery.not_assigned_courier", "You are not assigned to this delivery."));

        if (Status != DeliveryStatus.Assigned)
            return InvalidTransition(DeliveryStatus.AtStore);

        var previous = Status;
        Status = DeliveryStatus.AtStore;
        ArrivedStoreAt = utcNow;
        UpdatedAt = utcNow;
        Raise(new DeliveryStatusChangedEvent(Id, OrderId, courierId, previous, Status));
        return Result.Success();
    }

    public Result MarkPickedUp(Guid courierId, DateTimeOffset utcNow)
    {
        if (CourierId != courierId)
            return Result.Failure(Error.Forbidden("delivery.not_assigned_courier", "You are not assigned to this delivery."));

        if (Status is not (DeliveryStatus.Assigned or DeliveryStatus.AtStore))
            return InvalidTransition(DeliveryStatus.PickedUp);

        var previous = Status;
        Status = DeliveryStatus.PickedUp;
        ArrivedStoreAt ??= utcNow;
        PickedUpAt = utcNow;
        UpdatedAt = utcNow;
        Raise(new DeliveryStatusChangedEvent(Id, OrderId, courierId, previous, Status));
        return Result.Success();
    }

    public Result MarkArrivedAtCustomer(Guid courierId, DateTimeOffset utcNow)
    {
        if (CourierId != courierId)
            return Result.Failure(Error.Forbidden("delivery.not_assigned_courier", "You are not assigned to this delivery."));

        if (Status != DeliveryStatus.PickedUp)
            return InvalidTransition(DeliveryStatus.AtCustomer);

        var previous = Status;
        Status = DeliveryStatus.AtCustomer;
        ArrivedCustomerAt = utcNow;
        UpdatedAt = utcNow;
        Raise(new DeliveryStatusChangedEvent(Id, OrderId, courierId, previous, Status));
        return Result.Success();
    }

    public Result MarkDelivered(Guid courierId, decimal? codCollectedAmount, string? proofPhotoRef, DateTimeOffset utcNow)
    {
        if (CourierId != courierId)
            return Result.Failure(Error.Forbidden("delivery.not_assigned_courier", "You are not assigned to this delivery."));

        if (Status is not (DeliveryStatus.PickedUp or DeliveryStatus.AtCustomer))
            return InvalidTransition(DeliveryStatus.Delivered);

        var previous = Status;
        Status = DeliveryStatus.Delivered;
        ArrivedCustomerAt ??= utcNow;
        DeliveredAt = utcNow;
        CodCollectedAmount = codCollectedAmount;
        ProofPhotoRef ??= proofPhotoRef;
        UpdatedAt = utcNow;
        Raise(new DeliveryStatusChangedEvent(Id, OrderId, courierId, previous, Status));
        return Result.Success();
    }

    public Result MarkFailed(Guid courierId, DeliveryFailureReason reasonCode, string? failureNote, string? proofPhotoRef, DateTimeOffset utcNow)
    {
        if (CourierId != courierId)
            return Result.Failure(Error.Forbidden("delivery.not_assigned_courier", "You are not assigned to this delivery."));

        if (Status is not (DeliveryStatus.Assigned or DeliveryStatus.AtStore or DeliveryStatus.PickedUp or DeliveryStatus.AtCustomer))
            return InvalidTransition(DeliveryStatus.Failed);

        var previous = Status;
        Status = DeliveryStatus.Failed;
        FailedAt = utcNow;
        FailureReasonCode = reasonCode;
        FailureNote = failureNote;
        ProofPhotoRef ??= proofPhotoRef;
        UpdatedAt = utcNow;
        Raise(new DeliveryFailedEvent(Id, OrderId, courierId, reasonCode, failureNote));
        Raise(new DeliveryStatusChangedEvent(Id, OrderId, courierId, previous, Status));
        return Result.Success();
    }

    public Result UnassignForReassignment(DateTimeOffset utcNow)
    {
        if (Status is not (DeliveryStatus.Assigned or DeliveryStatus.AtStore))
            return InvalidTransition(DeliveryStatus.Unassigned);

        var previous = Status;
        CourierId = null;
        Status = DeliveryStatus.Unassigned;
        AssignedAt = null;
        ArrivedStoreAt = null;
        UpdatedAt = utcNow;
        Raise(new DeliveryStatusChangedEvent(Id, OrderId, null, previous, Status));
        return Result.Success();
    }

    private Result InvalidTransition(DeliveryStatus target)
        => Result.Failure(Error.Conflict("delivery.invalid_transition", $"Cannot transition delivery from '{Status}' to '{target}'."));
}
