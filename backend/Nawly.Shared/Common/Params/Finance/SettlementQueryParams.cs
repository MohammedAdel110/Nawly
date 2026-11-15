using Nawly.Shared.EnumHelper.FinanceEnums;

namespace Nawly.Shared.Common.Params.Finance;

public class SettlementQueryParams : BaseQueryParam
{
    public SettlementPartyType? PartyType { get; set; }
    public Guid? PartyId { get; set; }
    public SettlementStatus? Status { get; set; }
}
