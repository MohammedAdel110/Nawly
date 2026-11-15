using Nawly.Domain.Contracts.SpecificationPattern.BaseSpec;
using Nawly.Domain.Models.Finance;
using Nawly.Shared.Common.Params.Finance;
using Nawly.Shared.EnumHelper.FinanceEnums;

namespace Nawly.Services.Specifications.FinanceSpec;

public sealed class SettlementsFilterSpecification : BaseSpecifications<Settlement, Guid>
{
    public SettlementsFilterSpecification(SettlementQueryParams queryParams)
    {
        var partyType = queryParams.PartyType;
        var partyId = queryParams.PartyId;
        var status = queryParams.Status;

        Criteria = s => !s.IsDeleted
            && (partyType == null || s.PartyType == partyType)
            && (partyId == null || s.PartyId == partyId)
            && (status == null || s.Status == status);

        AddInclude(s => s.Lines);
        AddOrderBy(s => s.CreatedAt, isDescending: true);
        ApplyPaging(queryParams.PageSize, queryParams.PageIndex);
        ApplyAsNoTracking();
        ApplySplitQuery();
    }
}

public sealed class SettlementsCountSpecification : BaseSpecifications<Settlement, Guid>
{
    public SettlementsCountSpecification(SettlementQueryParams queryParams)
    {
        var partyType = queryParams.PartyType;
        var partyId = queryParams.PartyId;
        var status = queryParams.Status;

        Criteria = s => !s.IsDeleted
            && (partyType == null || s.PartyType == partyType)
            && (partyId == null || s.PartyId == partyId)
            && (status == null || s.Status == status);
    }
}

public sealed class SettlementWithLinesSpecification : BaseSpecifications<Settlement, Guid>
{
    public SettlementWithLinesSpecification(Guid settlementId, bool asNoTracking = false)
        : base(s => s.Id == settlementId && !s.IsDeleted)
    {
        AddInclude(s => s.Lines);
        ApplySplitQuery();
        if (asNoTracking)
            ApplyAsNoTracking();
    }
}

public sealed class LedgerEntriesByAccountSpecification : BaseSpecifications<LedgerEntry, Guid>
{
    public LedgerEntriesByAccountSpecification(LedgerAccountType? accountType, Guid? accountId)
        : base(e => !e.IsDeleted
            && (accountType == null || e.AccountType == accountType)
            && (accountId == null || e.AccountId == accountId))
    {
        AddOrderBy(e => e.CreatedAt, isDescending: true);
        ApplyAsNoTracking();
    }
}
