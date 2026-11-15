using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nawly.Domain.Models.Promotions;

namespace Nawly.Persistence.Configurations.Promotions;

public sealed class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.ToTable("coupons");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Code).IsRequired().HasMaxLength(40);
        builder.Property(c => c.Value).HasPrecision(12, 2);
        builder.Property(c => c.MaxDiscount).HasPrecision(12, 2);
        builder.Property(c => c.MinOrderAmount).HasPrecision(12, 2);
        builder.Property(c => c.CreatedBy).HasMaxLength(100);
        builder.Property(c => c.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(c => c.Code)
            .IsUnique()
            .HasFilter("is_deleted = false");

        builder.HasOne(c => c.Store)
            .WithMany()
            .HasForeignKey(c => c.StoreId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CouponRedemptionConfiguration : IEntityTypeConfiguration<CouponRedemption>
{
    public void Configure(EntityTypeBuilder<CouponRedemption> builder)
    {
        builder.ToTable("coupon_redemptions");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.Amount).HasPrecision(12, 2);
        builder.Property(r => r.CreatedBy).HasMaxLength(100);
        builder.Property(r => r.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(r => r.OrderId).IsUnique();
        builder.HasIndex(r => new { r.CouponId, r.UserId });

        builder.HasOne(r => r.Coupon)
            .WithMany(c => c.Redemptions)
            .HasForeignKey(r => r.CouponId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Order)
            .WithMany()
            .HasForeignKey(r => r.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
