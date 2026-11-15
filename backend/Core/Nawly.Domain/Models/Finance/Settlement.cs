using Nawly.Domain.Models.Common;
using Nawly.Shared.Common.Results;
using Nawly.Shared.EnumHelper.FinanceEnums;

namespace Nawly.Domain.Models.Finance;

public class Settlement : AggregateRoot
{
    public SettlementPartyType PartyType { get; set; }
    public Guid PartyId { get; set; }
    public DateTimeOffset PeriodStart { get; set; }
    public DateTimeOffset PeriodEnd { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal AdjustmentsAmount { get; set; }
    public decimal NetAmount { get; set; }
    public SettlementStatus Status { get; private set; } = SettlementStatus.Draft;
    public DateTimeOffset? PaidAt { get; private set; }
    public string? PaymentReference { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public uint RowVersion { get; set; }

    public ICollection<SettlementLine> Lines { get; set; } = [];
    public ICollection<LedgerEntry> LedgerEntries { get; set; } = [];

    public Result Approve(Guid adminUserId, DateTimeOffset utcNow)
    {
        if (Status != SettlementStatus.Draft)
            return Result.Failure(Error.Conflict("settlement.invalid_transition", "Only draft settlements can be approved."));

        Status = SettlementStatus.Approved;
        ApprovedBy = adminUserId;
        UpdatedAt = utcNow;
        return Result.Success();
    }

    public Result MarkPaid(string paymentReference, DateTimeOffset utcNow)
    {
        if (Status != SettlementStatus.Approved)
            return Result.Failure(Error.Conflict("settlement.invalid_transition", "Only approved settlements can be marked paid."));

        if (string.IsNullOrWhiteSpace(paymentReference))
            return Result.Failure(Error.Validation("settlement.reference_required", "Payment reference is required."));

        Status = SettlementStatus.Paid;
        PaymentReference = paymentReference.Trim();
        PaidAt = utcNow;
        UpdatedAt = utcNow;
        return Result.Success();
    }
}
