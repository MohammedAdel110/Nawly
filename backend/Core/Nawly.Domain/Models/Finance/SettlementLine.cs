using Nawly.Domain.Models.Common;

namespace Nawly.Domain.Models.Finance;

public class SettlementLine : BaseGuidEntity
{
    public Guid SettlementId { get; set; }
    public Guid? OrderId { get; set; }
    public Guid LedgerEntryId { get; set; }
    public decimal Amount { get; set; }

    public Settlement Settlement { get; set; } = null!;
    public LedgerEntry LedgerEntry { get; set; } = null!;
}
