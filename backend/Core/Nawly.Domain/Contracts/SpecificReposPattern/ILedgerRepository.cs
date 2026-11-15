using Nawly.Domain.Contracts.GenericReposPattern;
using Nawly.Domain.Models.Finance;
using Nawly.Shared.EnumHelper.FinanceEnums;

namespace Nawly.Domain.Contracts.SpecificReposPattern;

public interface ILedgerRepository : IGenericRepo<LedgerEntry, Guid>
{
    Task<IReadOnlyList<LedgerEntry>> GetUnsettledEntriesAsync(
        LedgerAccountType accountType,
        Guid accountId,
        DateTimeOffset periodStartUtc,
        DateTimeOffset periodEndUtc,
        CancellationToken cancellationToken = default);

    Task<decimal> GetCourierCashBalanceAsync(Guid courierId, CancellationToken cancellationToken = default);
    Task<decimal> GetStoreBalanceSummaryAsync(Guid storeId, CancellationToken cancellationToken = default);
}
