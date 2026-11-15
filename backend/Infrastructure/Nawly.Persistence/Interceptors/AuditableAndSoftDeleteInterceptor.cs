using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Nawly.Abstraction.IServices.Auth;
using Nawly.Domain.Contracts;
using Nawly.Domain.Models.Catalog;
using Nawly.Domain.Models.Finance;
using Nawly.Domain.Models.Ordering;
using Nawly.Domain.Models.Platform;

namespace Nawly.Persistence.Interceptors;

/// <summary>
/// Automatically stamps audit metadata, enforces soft deletes on ISoftDeletable entities,
/// protects append-only financial/audit tables from physical deletion, and maintains normalized Arabic search text.
/// </summary>
public sealed class AuditableAndSoftDeleteInterceptor(
    IClock clock,
    ICurrentUser? currentUser = null) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyRules(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyRules(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyRules(DbContext? context)
    {
        if (context is null)
            return;

        var utcNow = clock.UtcNow;
        var actorId = currentUser?.UserId?.ToString();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Deleted &&
                entry.Entity is LedgerEntry or AuditLog or OrderStatusHistory)
            {
                throw new InvalidOperationException(
                    $"Append-only entity '{entry.Entity.GetType().Name}' records cannot be deleted.");
            }

            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                switch (entry.Entity)
                {
                    case Store store:
                        store.RefreshNormalizedSearchText();
                        break;
                    case Product product:
                        product.RefreshNormalizedSearchText();
                        break;
                }
            }

            if (entry.Entity is IAuditable auditable)
            {
                if (entry.State == EntityState.Added)
                {
                    if (auditable.CreatedAt == default)
                        auditable.CreatedAt = utcNow;
                    auditable.CreatedBy ??= actorId;
                }
                else if (entry.State == EntityState.Modified)
                {
                    auditable.UpdatedAt = utcNow;
                    auditable.UpdatedBy = actorId;
                }
            }

            if (entry.State == EntityState.Deleted && entry.Entity is ISoftDeletable softDeletable)
            {
                entry.State = EntityState.Modified;
                softDeletable.IsDeleted = true;
                softDeletable.DeletedAt = utcNow;

                if (entry.Entity is IAuditable deletedAuditable)
                {
                    deletedAuditable.UpdatedAt = utcNow;
                    deletedAuditable.UpdatedBy = actorId;
                }
            }
        }
    }
}
