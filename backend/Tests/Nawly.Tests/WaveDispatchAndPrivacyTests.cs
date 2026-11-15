using Nawly.Abstraction.IServices.Auth;
using Nawly.Abstraction.IServices.Dispatch;
using Nawly.Domain.Models.Catalog;
using Nawly.Domain.Models.Ordering;
using Nawly.Services.Mapping.OrderMapping;
using Nawly.Services.ServicesImplementation.Dispatch.StrategyPattern;
using Nawly.Shared.EnumHelper.OrderEnums;
using Nawly.Shared.EnumHelper.PaymentEnums;

namespace Nawly.Tests;

public sealed class WaveDispatchAndPrivacyTests
{
    private static readonly DateTimeOffset FixedUtcNow = new(2026, 11, 15, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public void WaveDispatchStrategy_FiltersBusyCouriersRanksByScoreAndSplitsWaves()
    {
        var strategy = new WaveDispatchStrategy(new FixedClock(FixedUtcNow));
        const double storeLat = 30.2289;
        const double storeLng = 31.2367;

        var busyCourierId = Guid.NewGuid();
        var candidates = new List<DispatchCandidate>
        {
            new(busyCourierId, storeLat, storeLng, 5.0m, null, ActiveDeliveriesCount: 2, MaxConcurrentDeliveries: 2),
            new(Guid.NewGuid(), storeLat + 0.001, storeLng + 0.001, 4.9m, FixedUtcNow.AddMinutes(-90), 0, 2),
            new(Guid.NewGuid(), storeLat + 0.003, storeLng + 0.002, 4.7m, FixedUtcNow.AddMinutes(-60), 0, 2),
            new(Guid.NewGuid(), storeLat + 0.005, storeLng + 0.004, 4.5m, FixedUtcNow.AddMinutes(-30), 0, 2),
            new(Guid.NewGuid(), storeLat + 0.010, storeLng + 0.008, 4.2m, FixedUtcNow.AddMinutes(-20), 0, 2),
            new(Guid.NewGuid(), storeLat + 0.015, storeLng + 0.012, 4.0m, FixedUtcNow.AddMinutes(-10), 0, 2),
            new(Guid.NewGuid(), storeLat + 0.020, storeLng + 0.015, 4.0m, FixedUtcNow.AddMinutes(-5), 0, 2),
            new(Guid.NewGuid(), storeLat + 0.025, storeLng + 0.018, 4.0m, FixedUtcNow, 0, 2),
            new(Guid.NewGuid(), storeLat + 0.030, storeLng + 0.020, 4.0m, FixedUtcNow, 0, 2),
            new(Guid.NewGuid(), storeLat + 0.040, storeLng + 0.030, 3.8m, FixedUtcNow, 0, 2)
        };

        var wave1 = strategy.SelectCandidatesForWave(candidates, storeLat, storeLng, wave: 1);
        var wave2 = strategy.SelectCandidatesForWave(candidates, storeLat, storeLng, wave: 2);
        var wave3 = strategy.SelectCandidatesForWave(candidates, storeLat, storeLng, wave: 3);

        Assert.Equal(3, wave1.Count);
        Assert.Equal(5, wave2.Count);
        Assert.Single(wave3);

        var allSelected = wave1.Concat(wave2).Concat(wave3).ToList();
        Assert.DoesNotContain(busyCourierId, allSelected);
        Assert.Equal(candidates[1].CourierId, wave1[0]);
    }

    [Fact]
    public void OrderDetailsDto_MasksConfirmationCodeAndCourierPhoneByRoleAndStatus()
    {
        var order = Order.CreateNew(
            "NW-2026-0099",
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentMethod.Cash,
            FulfilmentType.Delivery,
            Guid.NewGuid(),
            FixedUtcNow);
        order.Store = new Store { Name = "Al-Nawa Bakery", NameAr = "مخبز النوى" };
        order.DeliveryNotes = "Call upon arrival";
        order.DeliveryConfirmationCode = "7391";

        order.Accept(Guid.NewGuid(), 15, 10, FixedUtcNow.AddMinutes(1));
        order.MarkReadyForPickup(Guid.NewGuid(), FixedUtcNow.AddMinutes(10));
        order.MarkPickedUp(Guid.NewGuid(), OrderActorRole.Courier, Guid.NewGuid(), FixedUtcNow.AddMinutes(12));

        var customerViewPickedUp = order.ToDetailsDto(OrderActorRole.Customer, "Ahmed", "+201000000001");
        var storeViewPickedUp = order.ToDetailsDto(OrderActorRole.Store, "Ahmed", "+201000000001");
        var courierViewPickedUp = order.ToDetailsDto(OrderActorRole.Courier, "Ahmed", "+201000000001");

        Assert.Equal("7391", customerViewPickedUp.DeliveryConfirmationCode);
        Assert.Equal("+201000000001", customerViewPickedUp.CourierPhone);
        Assert.Equal("Call upon arrival", customerViewPickedUp.DeliveryNotes);

        Assert.Null(storeViewPickedUp.DeliveryConfirmationCode);
        Assert.Null(storeViewPickedUp.CourierPhone);
        Assert.Null(storeViewPickedUp.DeliveryNotes);

        Assert.Null(courierViewPickedUp.DeliveryConfirmationCode);

        order.MarkDelivered(Guid.NewGuid(), OrderActorRole.Courier, Guid.NewGuid(), FixedUtcNow.AddMinutes(22));
        var customerViewDelivered = order.ToDetailsDto(OrderActorRole.Customer, "Ahmed", "+201000000001");
        var adminViewDelivered = order.ToDetailsDto(OrderActorRole.Admin, "Ahmed", "+201000000001");

        Assert.Null(customerViewDelivered.DeliveryConfirmationCode);
        Assert.Null(customerViewDelivered.CourierPhone);
        Assert.Equal("+201000000001", adminViewDelivered.CourierPhone);
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow => utcNow;
        public DateTimeOffset CairoNow => utcNow.ToOffset(TimeSpan.FromHours(2));
        public TimeOnly CairoLocalTime => TimeOnly.FromDateTime(CairoNow.DateTime);
        public DayOfWeek CairoDayOfWeek => CairoNow.DayOfWeek;
        public DateOnly CairoDate => DateOnly.FromDateTime(CairoNow.DateTime);
        public DateTimeOffset ToCairoTime(DateTimeOffset utcTime) => utcTime.ToOffset(TimeSpan.FromHours(2));
    }
}
