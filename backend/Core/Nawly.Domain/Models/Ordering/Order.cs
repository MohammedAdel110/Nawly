using Nawly.Domain.Events;
using Nawly.Domain.Models.Catalog;
using Nawly.Domain.Models.Common;
using Nawly.Domain.Models.Identity;
using Nawly.Domain.ValueObjects;
using Nawly.Shared.Common.Results;
using Nawly.Shared.EnumHelper.OrderEnums;
using Nawly.Shared.EnumHelper.PaymentEnums;

namespace Nawly.Domain.Models.Ordering;

public class Order : AggregateRoot
{
    private readonly List<OrderItem> _items = [];
    private readonly List<OrderStatusHistory> _statusHistory = [];

    public string OrderNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Guid StoreId { get; set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Placed;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
    public FulfilmentType FulfilmentType { get; set; } = FulfilmentType.Delivery;

    // Money fields (server-authoritative snapshot, SRS §5.2 & §5.4)
    public decimal Subtotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal ServiceFee { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }
    public decimal CashRounding { get; set; }
    public Guid? CouponId { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal CommissionAmount { get; set; }
    public string Currency { get; set; } = Money.DefaultCurrency;
    public string? CustomerNote { get; set; }
    public bool LeaveAtDoor { get; set; }

    // Immutable customer & address snapshots
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string AddressLabel { get; set; } = string.Empty;
    public string AreaName { get; set; } = string.Empty;
    public string AddressDescription { get; set; } = string.Empty;
    public string? Building { get; set; }
    public string? Floor { get; set; }
    public string? Apartment { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? DeliveryNotes { get; set; }

    // Lifecycle timestamps
    public DateTimeOffset? EstimatedReadyAt { get; set; }
    public DateTimeOffset? EstimatedDeliveryAt { get; set; }
    public DateTimeOffset PlacedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? AcceptedAt { get; private set; }
    public DateTimeOffset? PreparingAt { get; private set; }
    public DateTimeOffset? ReadyAt { get; private set; }
    public DateTimeOffset? PickedUpAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public OrderActorRole? CancelledBy { get; private set; }
    public CancelReasonCode? CancelReasonCode { get; private set; }
    public string? CancelReasonText { get; private set; }
    public string? DeliveryConfirmationCodeHash { get; set; }
    public Guid IdempotencyKey { get; set; }
    public uint RowVersion { get; set; }

    public CustomerProfile Customer { get; set; } = null!;
    public Store Store { get; set; } = null!;
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    public IReadOnlyCollection<OrderStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    public bool IsTerminal => Status is OrderStatus.Delivered or OrderStatus.Rejected or OrderStatus.Cancelled or OrderStatus.Failed;

    public static Order CreateNew(
        string orderNumber,
        Guid customerId,
        Guid storeId,
        PaymentMethod paymentMethod,
        FulfilmentType fulfilmentType,
        Guid idempotencyKey,
        DateTimeOffset utcNow)
    {
        var initialStatus = paymentMethod == PaymentMethod.Cash ? OrderStatus.Placed : OrderStatus.PendingPayment;
        var initialPaymentStatus = paymentMethod == PaymentMethod.Cash ? PaymentStatus.Unpaid : PaymentStatus.Pending;

        var order = new Order
        {
            OrderNumber = orderNumber,
            CustomerId = customerId,
            StoreId = storeId,
            Status = initialStatus,
            PaymentMethod = paymentMethod,
            PaymentStatus = initialPaymentStatus,
            FulfilmentType = fulfilmentType,
            IdempotencyKey = idempotencyKey,
            PlacedAt = utcNow,
            CreatedAt = utcNow
        };

        order._statusHistory.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            FromStatus = initialStatus,
            ToStatus = initialStatus,
            ActorUserId = customerId,
            ActorRole = OrderActorRole.Customer,
            At = utcNow
        });

        if (initialStatus == OrderStatus.Placed)
            order.Raise(new OrderPlacedEvent(order.Id, storeId, customerId, paymentMethod));

        return order;
    }

    public void AddItem(OrderItem item)
    {
        item.OrderId = Id;
        _items.Add(item);
    }

    public Result ConfirmOnlinePayment(DateTimeOffset utcNow)
    {
        if (Status != OrderStatus.PendingPayment)
            return InvalidTransition(OrderStatus.Placed);

        PaymentStatus = PaymentStatus.Paid;
        Transition(OrderStatus.Placed, null, OrderActorRole.System, null, null, utcNow);
        Raise(new OrderPlacedEvent(Id, StoreId, CustomerId, PaymentMethod));
        return Result.Success();
    }

    public Result Accept(Guid actorUserId, int prepMinutes, int travelMinutes, DateTimeOffset utcNow)
    {
        if (Status != OrderStatus.Placed)
            return InvalidTransition(OrderStatus.Accepted);

        if (prepMinutes < 1)
            return Result.Failure(Error.Validation("order.invalid_prep_time", "Preparation time must be at least 1 minute."));

        Transition(OrderStatus.Accepted, actorUserId, OrderActorRole.Store, null, null, utcNow);
        AcceptedAt = utcNow;
        EstimatedReadyAt = utcNow.AddMinutes(prepMinutes);
        EstimatedDeliveryAt = utcNow.AddMinutes(prepMinutes + travelMinutes + 5);
        Raise(new OrderAcceptedEvent(Id, StoreId, CustomerId, prepMinutes));
        return Result.Success();
    }

    public Result Reject(Guid actorUserId, CancelReasonCode reasonCode, string? reasonText, DateTimeOffset utcNow)
    {
        if (Status != OrderStatus.Placed)
            return InvalidTransition(OrderStatus.Rejected);

        Transition(OrderStatus.Rejected, actorUserId, OrderActorRole.Store, reasonCode, reasonText, utcNow);
        CancelledAt = utcNow;
        CancelledBy = OrderActorRole.Store;
        CancelReasonCode = reasonCode;
        CancelReasonText = reasonText;
        Raise(new OrderRejectedEvent(Id, StoreId, CustomerId, reasonCode, reasonText));
        return Result.Success();
    }

    public Result StartPreparing(Guid actorUserId, DateTimeOffset utcNow)
    {
        if (Status != OrderStatus.Accepted)
            return InvalidTransition(OrderStatus.Preparing);

        Transition(OrderStatus.Preparing, actorUserId, OrderActorRole.Store, null, null, utcNow);
        PreparingAt = utcNow;
        Raise(new OrderPreparingEvent(Id, StoreId, CustomerId));
        return Result.Success();
    }

    public Result MarkReadyForPickup(Guid actorUserId, DateTimeOffset utcNow)
    {
        if (Status is not (OrderStatus.Accepted or OrderStatus.Preparing))
            return InvalidTransition(OrderStatus.ReadyForPickup);

        Transition(OrderStatus.ReadyForPickup, actorUserId, OrderActorRole.Store, null, null, utcNow);
        PreparingAt ??= utcNow;
        ReadyAt = utcNow;
        Raise(new OrderReadyForPickupEvent(Id, StoreId, CustomerId, FulfilmentType));
        return Result.Success();
    }

    public Result MarkPickedUp(Guid actorUserId, OrderActorRole actorRole, Guid? courierId, DateTimeOffset utcNow)
    {
        if (Status != OrderStatus.ReadyForPickup)
            return InvalidTransition(OrderStatus.PickedUp);

        Transition(OrderStatus.PickedUp, actorUserId, actorRole, null, null, utcNow);
        PickedUpAt = utcNow;
        Raise(new OrderPickedUpEvent(Id, StoreId, CustomerId, courierId));
        return Result.Success();
    }

    public Result MarkDelivered(Guid actorUserId, OrderActorRole actorRole, Guid? courierId, DateTimeOffset utcNow)
    {
        var allowed = FulfilmentType == FulfilmentType.Pickup
            ? Status == OrderStatus.ReadyForPickup
            : Status == OrderStatus.PickedUp;

        if (!allowed)
            return InvalidTransition(OrderStatus.Delivered);

        Transition(OrderStatus.Delivered, actorUserId, actorRole, null, null, utcNow);
        DeliveredAt = utcNow;
        if (PaymentMethod == PaymentMethod.Cash)
            PaymentStatus = PaymentStatus.Paid;

        Raise(new OrderDeliveredEvent(Id, StoreId, CustomerId, courierId, PaymentMethod, Total));
        return Result.Success();
    }

    public Result MarkFailed(Guid actorUserId, OrderActorRole actorRole, CancelReasonCode reasonCode, string? reasonText, DateTimeOffset utcNow)
    {
        if (Status != OrderStatus.PickedUp)
            return InvalidTransition(OrderStatus.Failed);

        Transition(OrderStatus.Failed, actorUserId, actorRole, reasonCode, reasonText, utcNow);
        CancelReasonCode = reasonCode;
        CancelReasonText = reasonText;
        Raise(new OrderFailedEvent(Id, StoreId, CustomerId, reasonText));
        return Result.Success();
    }

    public Result Cancel(Guid? actorUserId, OrderActorRole actorRole, CancelReasonCode reasonCode, string? reasonText, DateTimeOffset utcNow)
    {
        if (IsTerminal)
            return InvalidTransition(OrderStatus.Cancelled);

        if (actorRole == OrderActorRole.Customer && Status is not (OrderStatus.PendingPayment or OrderStatus.Placed))
            return Result.Failure(Error.Conflict("order.cancellation_requires_approval", "Order can only be cancelled freely before store acceptance."));

        if (actorRole == OrderActorRole.Store && Status is not (OrderStatus.Accepted or OrderStatus.Preparing))
            return InvalidTransition(OrderStatus.Cancelled);

        var previous = Status;
        Transition(OrderStatus.Cancelled, actorUserId, actorRole, reasonCode, reasonText, utcNow);
        CancelledAt = utcNow;
        CancelledBy = actorRole;
        CancelReasonCode = reasonCode;
        CancelReasonText = reasonText;
        Raise(new OrderCancelledEvent(Id, StoreId, CustomerId, actorRole, reasonCode, previous));
        return Result.Success();
    }

    private void Transition(
        OrderStatus toStatus,
        Guid? actorUserId,
        OrderActorRole actorRole,
        CancelReasonCode? reasonCode,
        string? reasonText,
        DateTimeOffset utcNow)
    {
        var fromStatus = Status;
        Status = toStatus;
        UpdatedAt = utcNow;
        _statusHistory.Add(new OrderStatusHistory
        {
            OrderId = Id,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            ActorUserId = actorUserId,
            ActorRole = actorRole,
            ReasonCode = reasonCode,
            ReasonText = reasonText,
            At = utcNow
        });
    }

    private Result InvalidTransition(OrderStatus target)
        => Result.Failure(Error.Conflict("order.invalid_transition", $"Cannot transition order from '{Status}' to '{target}'."));
}
