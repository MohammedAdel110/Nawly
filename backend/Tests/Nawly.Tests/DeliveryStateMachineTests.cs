using Nawly.Domain.Models.Delivery;
using Nawly.Shared.EnumHelper.DeliveryEnums;

namespace Nawly.Tests;

public sealed class DeliveryStateMachineTests
{
    private static readonly DateTimeOffset BaseTime = new(2026, 11, 15, 13, 0, 0, TimeSpan.Zero);

    [Fact]
    public void OfferAssignAndCompleteDelivery_TransitionsCleanlyAndRecordsCodAmount()
    {
        var winningCourierId = Guid.NewGuid();
        var otherCourierId = Guid.NewGuid();
        var delivery = new Delivery
        {
            OrderId = Guid.NewGuid(),
            DistanceKm = 2.4,
            CourierPayout = 18m,
            CodAmount = 215m
        };

        var offer1 = delivery.CreateOffer(winningCourierId, 1, BaseTime, TimeSpan.FromSeconds(45));
        var offer2 = delivery.CreateOffer(otherCourierId, 1, BaseTime, TimeSpan.FromSeconds(45));
        Assert.Equal(DeliveryStatus.Offered, delivery.Status);

        var assign = delivery.AssignCourier(winningCourierId, BaseTime.AddSeconds(10));
        Assert.True(assign.IsSuccess);
        Assert.Equal(DeliveryStatus.Assigned, delivery.Status);
        Assert.Equal(DeliveryOfferResponse.Accepted, offer1.Response);
        Assert.Equal(DeliveryOfferResponse.Cancelled, offer2.Response);

        Assert.True(delivery.MarkArrivedAtStore(winningCourierId, BaseTime.AddMinutes(4)).IsSuccess);
        Assert.Equal(DeliveryStatus.AtStore, delivery.Status);

        Assert.True(delivery.MarkPickedUp(winningCourierId, BaseTime.AddMinutes(6)).IsSuccess);
        Assert.Equal(DeliveryStatus.PickedUp, delivery.Status);

        Assert.True(delivery.MarkArrivedAtCustomer(winningCourierId, BaseTime.AddMinutes(14)).IsSuccess);
        Assert.Equal(DeliveryStatus.AtCustomer, delivery.Status);

        Assert.True(delivery.MarkDelivered(winningCourierId, 215m, null, BaseTime.AddMinutes(16)).IsSuccess);
        Assert.Equal(DeliveryStatus.Delivered, delivery.Status);
        Assert.Equal(215m, delivery.CodCollectedAmount);
    }

    [Fact]
    public void NonAssignedCourier_CannotAdvanceDelivery()
    {
        var assignedCourierId = Guid.NewGuid();
        var intruderCourierId = Guid.NewGuid();
        var delivery = new Delivery { OrderId = Guid.NewGuid() };
        delivery.AssignCourier(assignedCourierId, BaseTime);

        var result = delivery.MarkArrivedAtStore(intruderCourierId, BaseTime.AddMinutes(2));

        Assert.True(result.IsFailure);
        Assert.Equal("delivery.not_assigned_courier", result.Error.Code);
        Assert.Equal(DeliveryStatus.Assigned, delivery.Status);
    }

    [Fact]
    public void UnassignForReassignment_AllowedBeforePickup_BlockedAfterPickup()
    {
        var courierId = Guid.NewGuid();
        var delivery = new Delivery { OrderId = Guid.NewGuid() };
        delivery.AssignCourier(courierId, BaseTime);

        var unassign = delivery.UnassignForReassignment(BaseTime.AddMinutes(2));
        Assert.True(unassign.IsSuccess);
        Assert.Null(delivery.CourierId);
        Assert.Equal(DeliveryStatus.Unassigned, delivery.Status);

        delivery.AssignCourier(courierId, BaseTime.AddMinutes(3));
        delivery.MarkPickedUp(courierId, BaseTime.AddMinutes(5));

        var blocked = delivery.UnassignForReassignment(BaseTime.AddMinutes(6));
        Assert.True(blocked.IsFailure);
        Assert.Equal("delivery.invalid_transition", blocked.Error.Code);
        Assert.Equal(DeliveryStatus.PickedUp, delivery.Status);
    }
}
