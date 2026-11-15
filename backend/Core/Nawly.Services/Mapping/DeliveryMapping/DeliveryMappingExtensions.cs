using Nawly.Domain.Models.Delivery;
using Nawly.Shared.DTOs.Delivery;

namespace Nawly.Services.Mapping.DeliveryMapping;

public static class DeliveryMappingExtensions
{
    public static DeliveryDto ToDto(this Delivery delivery) => new(
        delivery.Id,
        delivery.OrderId,
        delivery.Order?.OrderNumber ?? string.Empty,
        delivery.CourierId,
        delivery.Courier?.User?.FullName,
        delivery.Courier?.User?.PhoneNumber,
        delivery.Status,
        delivery.DistanceKm,
        delivery.CourierPayout,
        delivery.CodAmount,
        delivery.CodCollectedAmount,
        delivery.AssignedAt,
        delivery.ArrivedStoreAt,
        delivery.PickedUpAt,
        delivery.ArrivedCustomerAt,
        delivery.DeliveredAt,
        delivery.FailedAt,
        delivery.FailureReasonCode,
        delivery.FailureNote,
        delivery.ProofPhotoRef);

    public static DeliveryOfferDto ToDto(this DeliveryOffer offer)
    {
        var delivery = offer.Delivery;
        var order = delivery?.Order;
        var store = order?.Store;

        return new DeliveryOfferDto(
            offer.Id,
            offer.DeliveryId,
            delivery?.OrderId ?? Guid.Empty,
            order?.OrderNumber ?? string.Empty,
            order?.StoreId ?? Guid.Empty,
            store?.Name ?? string.Empty,
            store?.NameAr ?? string.Empty,
            store?.AddressText ?? string.Empty,
            store?.Latitude,
            store?.Longitude,
            order?.AreaName ?? string.Empty,
            order?.AddressDescription ?? string.Empty,
            delivery?.DistanceKm ?? 0,
            delivery?.CourierPayout ?? 0m,
            delivery?.CodAmount ?? 0m,
            offer.Wave,
            offer.OfferedAt,
            offer.ExpiresAt,
            offer.Response);
    }
}
