using Nawly.Domain.Models.Identity;
using Nawly.Shared.DTOs.Courier;

namespace Nawly.Services.Mapping.CourierMapping;

public static class CourierMappingExtensions
{
    public static CourierProfileDto ToDto(this CourierProfile courier) => new(
        courier.Id,
        courier.UserId,
        courier.User?.FullName ?? string.Empty,
        courier.User?.PhoneNumber ?? string.Empty,
        courier.Status,
        courier.VehicleType,
        courier.PhotoRef,
        courier.IsOnline,
        courier.LastLat,
        courier.LastLng,
        courier.LastLocationAt,
        courier.MaxConcurrentDeliveries,
        courier.CashOnHand,
        courier.RatingAvg,
        courier.RatingCount,
        courier.LastDeliveredAt,
        courier.RejectionReason);
}
