using Nawly.Domain.Models.Finance;
using Nawly.Shared.DTOs.Finance;

namespace Nawly.Services.Mapping.FinanceMapping;

public static class FinanceMappingExtensions
{
    public static LedgerEntryDto ToDto(this LedgerEntry entry) => new(
        entry.Id,
        entry.AccountType,
        entry.AccountId,
        entry.OrderId,
        entry.DeliveryId,
        entry.PaymentId,
        entry.Kind,
        entry.Direction,
        entry.Amount,
        entry.Description,
        entry.SettlementId,
        entry.CreatedAt);

    public static SettlementLineDto ToDto(this SettlementLine line) => new(
        line.Id,
        line.OrderId,
        line.LedgerEntryId,
        line.Amount);

    public static SettlementDto ToDto(this Settlement settlement, string partyName = "") => new(
        settlement.Id,
        settlement.PartyType,
        settlement.PartyId,
        partyName,
        settlement.PeriodStart,
        settlement.PeriodEnd,
        settlement.GrossAmount,
        settlement.CommissionAmount,
        settlement.AdjustmentsAmount,
        settlement.NetAmount,
        settlement.Status,
        settlement.PaidAt,
        settlement.PaymentReference,
        settlement.ApprovedBy,
        settlement.CreatedAt,
        settlement.Lines.Where(l => !l.IsDeleted).Select(l => l.ToDto()).ToList());
}
