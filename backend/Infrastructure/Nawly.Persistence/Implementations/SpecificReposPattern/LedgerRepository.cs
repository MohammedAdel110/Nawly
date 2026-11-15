using Microsoft.EntityFrameworkCore;
using Nawly.Domain.Contracts.SpecificReposPattern;
using Nawly.Domain.Models.Finance;
using Nawly.Persistence.Implementations.ReposPattern;
using Nawly.Shared.EnumHelper.FinanceEnums;
using AppDbContext = Nawly.Persistence.NawlyDbContext.NawlyDbContext;

namespace Nawly.Persistence.Implementations.SpecificReposPattern;

public sealed class LedgerRepository(AppDbContext dbContext)
    : GenericRepo<LedgerEntry, Guid>(dbContext), ILedgerRepository
{
    public async Task<IReadOnlyList<LedgerEntry>> GetUnsettledEntriesAsync(
        LedgerAccountType accountType,
        Guid accountId,
        DateTimeOffset periodStartUtc,
        DateTimeOffset periodEndUtc,
        CancellationToken cancellationToken = default)
        => await DbSet
            .Where(e => e.AccountType == accountType
                && e.AccountId == accountId
                && e.SettlementId == null
                && e.CreatedAt >= periodStartUtc
                && e.CreatedAt <= periodEndUtc)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<decimal> GetCourierCashBalanceAsync(Guid courierId, CancellationToken cancellationToken = default)
        => await DbSet
            .Where(e => e.AccountType == LedgerAccountType.Courier
                && e.AccountId == courierId
                && e.SettlementId == null)
            .SumAsync(
                e => e.Direction == LedgerDirection.Debit ? e.Amount : -e.Amount,
                cancellationToken);

    public async Task<decimal> GetStoreBalanceSummaryAsync(Guid storeId, CancellationToken cancellationToken = default)
        => await DbSet
            .Where(e => e.AccountType == LedgerAccountType.Store
                && e.AccountId == storeId
                && e.SettlementId == null)
            .SumAsync(
                e => e.Direction == LedgerDirection.Credit ? e.Amount : -e.Amount,
                cancellationToken);
}
