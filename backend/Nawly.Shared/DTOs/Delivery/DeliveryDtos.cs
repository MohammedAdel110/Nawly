using Nawly.Shared.EnumHelper.DeliveryEnums;

namespace Nawly.Shared.DTOs.Delivery;

public sealed record DeliveryDto(
    Guid Id,
    Guid OrderId,
    string OrderNumber,
    Guid? CourierId,
    string? CourierName,
    string? CourierPhone,
    DeliveryStatus Status,
    double DistanceKm,
    decimal CourierPayout,
    decimal CodAmount,
    decimal? CodCollectedAmount,
    DateTimeOffset? AssignedAt,
    DateTimeOffset? ArrivedStoreAt,
    DateTimeOffset? PickedUpAt,
    DateTimeOffset? ArrivedCustomerAt,
    DateTimeOffset? DeliveredAt,
    DateTimeOffset? FailedAt,
    DeliveryFailureReason? FailureReasonCode,
    string? FailureNote,
    string? ProofPhotoRef);

public sealed record DeliveryOfferDto(
    Guid OfferId,
    Guid DeliveryId,
    Guid OrderId,
    string OrderNumber,
    Guid StoreId,
    string StoreName,
    string StoreNameAr,
    string StoreAddress,
    double? StoreLatitude,
    double? StoreLongitude,
    string AreaName,
    string DeliveryAddressSummary,
    double EstimatedDistanceKm,
    decimal CourierPayout,
    decimal CodAmount,
    int Wave,
    DateTimeOffset OfferedAt,
    DateTimeOffset ExpiresAt,
    DeliveryOfferResponse Response);

public sealed record FailDeliveryDto(DeliveryFailureReason ReasonCode, string? Note);

public sealed record ConfirmDeliveryCodeDto(string Code, decimal? CodCollectedAmount, string? ProofPhotoRef = null);
