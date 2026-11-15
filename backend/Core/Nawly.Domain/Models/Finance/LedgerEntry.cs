using Nawly.Domain.Models.Common;
using Nawly.Shared.EnumHelper.FinanceEnums;

namespace Nawly.Domain.Models.Finance;

/// <summary>
/// Immutable append-only financial ledger entry (SRS §5.7, §6.3).
/// Corrections are made exclusively via reversing entries.
/// </summary>
public class LedgerEntry : BaseGuidEntity
{
    public LedgerAccountType AccountType { get; set; }
    public Guid? AccountId { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? DeliveryId { get; set; }
    public Guid? PaymentId { get; set; }
    public LedgerEntryKind Kind { get; set; }
    public LedgerDirection Direction { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid? SettlementId { get; set; }

    public Settlement? Settlement { get; set; }
}
