using Nawly.Domain.Events;
using Nawly.Domain.Models.Ordering;
using Nawly.Shared.EnumHelper.OrderEnums;
using Nawly.Shared.EnumHelper.PaymentEnums;

namespace Nawly.Tests;

public sealed class OrderStateMachineTests
{
    private static readonly DateTimeOffset BaseTime = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateNew_CashOrder_StartsInPlacedAndUnpaidWithPlacedEvent()
    {
        var order = Order.CreateNew(
            "NW-2026-0001",
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentMethod.Cash,
            FulfilmentType.Delivery,
            Guid.NewGuid(),
            BaseTime);

        Assert.Equal(OrderStatus.Placed, order.Status);
        Assert.Equal(PaymentStatus.Unpaid, order.PaymentStatus);
        Assert.Single(order.StatusHistory);
        Assert.Contains(order.DomainEvents, e => e is OrderPlacedEvent);
    }

    [Fact]
    public void CreateNew_OnlineOrder_StartsPendingPaymentAndMovesToPlacedOnConfirmation()
    {
        var order = Order.CreateNew(
            "NW-2026-0002",
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentMethod.Card,
            FulfilmentType.Delivery,
            Guid.NewGuid(),
            BaseTime);

        Assert.Equal(OrderStatus.PendingPayment, order.Status);
        Assert.Equal(PaymentStatus.Pending, order.PaymentStatus);
        Assert.Empty(order.DomainEvents);

        var confirm = order.ConfirmOnlinePayment(BaseTime.AddMinutes(1));

        Assert.True(confirm.IsSuccess);
        Assert.Equal(OrderStatus.Placed, order.Status);
        Assert.Equal(PaymentStatus.Paid, order.PaymentStatus);
        Assert.Contains(order.DomainEvents, e => e is OrderPlacedEvent);
    }

    [Fact]
    public void FullDeliveryLifecycle_CashOrder_TransitionsCleanlyAndClearsCodeOnDelivery()
    {
        var storeActorId = Guid.NewGuid();
        var courierId = Guid.NewGuid();
        var order = Order.CreateNew(
            "NW-2026-0003",
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentMethod.Cash,
            FulfilmentType.Delivery,
            Guid.NewGuid(),
            BaseTime);
        order.DeliveryConfirmationCode = "4829";

        Assert.True(order.Accept(storeActorId, 20, 10, BaseTime.AddMinutes(1)).IsSuccess);
        Assert.Equal(OrderStatus.Accepted, order.Status);
        Assert.Equal(BaseTime.AddMinutes(21), order.EstimatedReadyAt);

        Assert.True(order.StartPreparing(storeActorId, BaseTime.AddMinutes(2)).IsSuccess);
        Assert.Equal(OrderStatus.Preparing, order.Status);

        Assert.True(order.MarkReadyForPickup(storeActorId, BaseTime.AddMinutes(15)).IsSuccess);
        Assert.Equal(OrderStatus.ReadyForPickup, order.Status);

        Assert.True(order.MarkPickedUp(courierId, OrderActorRole.Courier, courierId, BaseTime.AddMinutes(18)).IsSuccess);
        Assert.Equal(OrderStatus.PickedUp, order.Status);

        Assert.True(order.MarkDelivered(courierId, OrderActorRole.Courier, courierId, BaseTime.AddMinutes(28)).IsSuccess);
        Assert.Equal(OrderStatus.Delivered, order.Status);
        Assert.Equal(PaymentStatus.Paid, order.PaymentStatus);
        Assert.Null(order.DeliveryConfirmationCode);
        Assert.True(order.IsTerminal);
    }

    [Fact]
    public void CustomerCancel_AllowedWhenPlaced_RequiresApprovalAfterAcceptance()
    {
        var customerId = Guid.NewGuid();
        var placedOrder = Order.CreateNew(
            "NW-2026-0004",
            customerId,
            Guid.NewGuid(),
            PaymentMethod.Cash,
            FulfilmentType.Delivery,
            Guid.NewGuid(),
            BaseTime);

        var earlyCancel = placedOrder.Cancel(
            customerId,
            OrderActorRole.Customer,
            CancelReasonCode.CustomerChangedMind,
            "Changed my mind",
            BaseTime.AddMinutes(1));

        Assert.True(earlyCancel.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, placedOrder.Status);
        Assert.Equal(OrderActorRole.Customer, placedOrder.CancelledBy);

        var acceptedOrder = Order.CreateNew(
            "NW-2026-0005",
            customerId,
            Guid.NewGuid(),
            PaymentMethod.Cash,
            FulfilmentType.Delivery,
            Guid.NewGuid(),
            BaseTime);
        acceptedOrder.Accept(Guid.NewGuid(), 15, 10, BaseTime.AddMinutes(1));

        var lateCancel = acceptedOrder.Cancel(
            customerId,
            OrderActorRole.Customer,
            CancelReasonCode.CustomerChangedMind,
            null,
            BaseTime.AddMinutes(3));

        Assert.True(lateCancel.IsFailure);
        Assert.Equal("order.cancellation_requires_approval", lateCancel.Error.Code);
        Assert.Equal(OrderStatus.Accepted, acceptedOrder.Status);
    }

    [Fact]
    public void InvalidTransition_FromPlacedDirectlyToDelivered_ReturnsConflict()
    {
        var order = Order.CreateNew(
            "NW-2026-0006",
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentMethod.Cash,
            FulfilmentType.Delivery,
            Guid.NewGuid(),
            BaseTime);

        var result = order.MarkDelivered(Guid.NewGuid(), OrderActorRole.Courier, Guid.NewGuid(), BaseTime.AddMinutes(5));

        Assert.True(result.IsFailure);
        Assert.Equal("order.invalid_transition", result.Error.Code);
        Assert.Equal(OrderStatus.Placed, order.Status);
    }
}
