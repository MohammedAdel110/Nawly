using Nawly.Shared.EnumHelper.FinanceEnums;

namespace Nawly.Shared.DTOs.Finance;

public sealed record LedgerEntryDto(
    Guid Id,
    LedgerAccountType AccountType,
    Guid? AccountId,
    Guid? OrderId,
    Guid? DeliveryId,
    Guid? PaymentId,
    LedgerEntryKind Kind,
    LedgerDirection Direction,
    decimal Amount,
    string Description,
    Guid? SettlementId,
    DateTimeOffset CreatedAt);

public sealed record SettlementLineDto(
    Guid Id,
    Guid? OrderId,
    Guid LedgerEntryId,
    decimal Amount);

public sealed record SettlementDto(
    Guid Id,
    SettlementPartyType PartyType,
    Guid PartyId,
    string PartyName,
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    decimal GrossAmount,
    decimal CommissionAmount,
    decimal AdjustmentsAmount,
    decimal NetAmount,
    SettlementStatus Status,
    DateTimeOffset? PaidAt,
    string? PaymentReference,
    Guid? ApprovedBy,
    DateTimeOffset CreatedAt,
    IReadOnlyList<SettlementLineDto> Lines);

public sealed record GenerateSettlementsRequestDto(
    SettlementPartyType? PartyType,
    Guid? PartyId,
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd);

public sealed record MarkSettlementPaidDto(string PaymentReference);

public sealed record ReconcileCourierCashDto(
    Guid CourierId,
    decimal AmountReceived,
    string ReceiptReference,
    string? Note = null);
