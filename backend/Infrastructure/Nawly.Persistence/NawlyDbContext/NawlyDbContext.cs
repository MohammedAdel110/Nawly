using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Nawly.Domain.Contracts;
using Nawly.Domain.Models.Catalog;
using Nawly.Domain.Models.Common;
using Nawly.Domain.Models.Delivery;
using Nawly.Domain.Models.Engagement;
using Nawly.Domain.Models.Finance;
using Nawly.Domain.Models.Geography;
using Nawly.Domain.Models.Identity;
using Nawly.Domain.Models.Ordering;
using Nawly.Domain.Models.Payments;
using Nawly.Domain.Models.Platform;
using Nawly.Domain.Models.Promotions;

namespace Nawly.Persistence.Contexts;

public class NawlyDbContext(DbContextOptions<NawlyDbContext> options)
    : IdentityDbContext<AppUser, AppRole, Guid>(options)
{
    private static readonly MethodInfo SoftDeleteFilterMethod = typeof(NawlyDbContext)
        .GetMethod(nameof(ConfigureSoftDeleteFilter), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo IgnoreDomainEventsMethod = typeof(NawlyDbContext)
        .GetMethod(nameof(IgnoreDomainEvents), BindingFlags.NonPublic | BindingFlags.Static)!;

    // Identity
    public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();
    public DbSet<CourierProfile> CourierProfiles => Set<CourierProfile>();
    public DbSet<OtpChallenge> OtpChallenges => Set<OtpChallenge>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // Geography
    public DbSet<DeliveryZone> DeliveryZones => Set<DeliveryZone>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<Address> Addresses => Set<Address>();

    // Catalog
    public DbSet<StoreCategory> StoreCategories => Set<StoreCategory>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<StoreDeliveryZone> StoreDeliveryZones => Set<StoreDeliveryZone>();
    public DbSet<StoreOpeningHour> StoreOpeningHours => Set<StoreOpeningHour>();
    public DbSet<StoreStaff> StoreStaff => Set<StoreStaff>();
    public DbSet<MenuCategory> MenuCategories => Set<MenuCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductOptionGroup> ProductOptionGroups => Set<ProductOptionGroup>();
    public DbSet<ProductOption> ProductOptions => Set<ProductOption>();

    // Ordering
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<CartItemOption> CartItemOptions => Set<CartItemOption>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderItemOption> OrderItemOptions => Set<OrderItemOption>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();

    // Delivery
    public DbSet<Delivery> Deliveries => Set<Delivery>();
    public DbSet<DeliveryOffer> DeliveryOffers => Set<DeliveryOffer>();
    public DbSet<CourierShift> CourierShifts => Set<CourierShift>();
    public DbSet<CourierLocationHistory> CourierLocationHistories => Set<CourierLocationHistory>();

    // Payments & Promotions
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentWebhookEvent> PaymentWebhookEvents => Set<PaymentWebhookEvent>();
    public DbSet<Refund> Refunds => Set<Refund>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<CouponRedemption> CouponRedemptions => Set<CouponRedemption>();

    // Finance
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<Settlement> Settlements => Set<Settlement>();
    public DbSet<SettlementLine> SettlementLines => Set<SettlementLine>();

    // Engagement
    public DbSet<Banner> Banners => Set<Banner>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
    public DbSet<SupportMessage> SupportMessages => Set<SupportMessage>();
    public DbSet<WebPushSubscription> WebPushSubscriptions => Set<WebPushSubscription>();

    // Platform
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasPostgresExtension("pg_trgm");

        builder.Entity<AppUser>().ToTable("users");
        builder.Entity<AppRole>().ToTable("roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");

        builder.ApplyConfigurationsFromAssembly(typeof(NawlyDbContext).Assembly);

        foreach (var entityType in builder.Model.GetEntityTypes().ToList())
        {
            var clrType = entityType.ClrType;
            if (typeof(IHasDomainEvents).IsAssignableFrom(clrType))
                IgnoreDomainEventsMethod.MakeGenericMethod(clrType).Invoke(null, [builder]);

            if (typeof(ISoftDeletable).IsAssignableFrom(clrType))
                SoftDeleteFilterMethod.MakeGenericMethod(clrType).Invoke(null, [builder]);
        }
    }

    private static void ConfigureSoftDeleteFilter<TEntity>(ModelBuilder builder)
        where TEntity : class, ISoftDeletable
        => builder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);

    private static void IgnoreDomainEvents<TEntity>(ModelBuilder builder)
        where TEntity : class, IHasDomainEvents
        => builder.Entity<TEntity>().Ignore(e => e.DomainEvents);
}
