using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nawly.Domain.Models.Delivery;

namespace Nawly.Persistence.Configurations.Delivery;

public sealed class DeliveryConfiguration : IEntityTypeConfiguration<Domain.Models.Delivery.Delivery>
{
    public void Configure(EntityTypeBuilder<Domain.Models.Delivery.Delivery> builder)
    {
        builder.ToTable("deliveries");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.CourierPayout).HasPrecision(12, 2);
        builder.Property(d => d.CodAmount).HasPrecision(12, 2);
        builder.Property(d => d.CodCollectedAmount).HasPrecision(12, 2);
        builder.Property(d => d.FailureNote).HasMaxLength(500);
        builder.Property(d => d.ProofPhotoRef).HasMaxLength(300);
        builder.Property(d => d.PickupCodeHash).HasMaxLength(128);
        builder.Property(d => d.RowVersion).IsRowVersion();
        builder.Property(d => d.CreatedBy).HasMaxLength(100);
        builder.Property(d => d.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(d => d.OrderId).IsUnique();
        builder.HasIndex(d => new { d.CourierId, d.Status });
        builder.HasIndex(d => new { d.Status, d.CreatedAt });

        builder.HasOne(d => d.Order)
            .WithOne()
            .HasForeignKey<Domain.Models.Delivery.Delivery>(d => d.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Courier)
            .WithMany()
            .HasForeignKey(d => d.CourierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DeliveryOfferConfiguration : IEntityTypeConfiguration<DeliveryOffer>
{
    public void Configure(EntityTypeBuilder<DeliveryOffer> builder)
    {
        builder.ToTable("delivery_offers");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();
        builder.Property(o => o.CreatedBy).HasMaxLength(100);
        builder.Property(o => o.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(o => new { o.CourierId, o.Response, o.ExpiresAt });
        builder.HasIndex(o => new { o.DeliveryId, o.Wave });

        builder.HasOne(o => o.Delivery)
            .WithMany(d => d.Offers)
            .HasForeignKey(o => o.DeliveryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(o => o.Courier)
            .WithMany()
            .HasForeignKey(o => o.CourierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CourierShiftConfiguration : IEntityTypeConfiguration<CourierShift>
{
    public void Configure(EntityTypeBuilder<CourierShift> builder)
    {
        builder.ToTable("courier_shifts");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.EndReason).HasMaxLength(200);
        builder.Property(s => s.CreatedBy).HasMaxLength(100);
        builder.Property(s => s.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(s => new { s.CourierId, s.EndedAt });

        builder.HasOne(s => s.Courier)
            .WithMany()
            .HasForeignKey(s => s.CourierId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CourierLocationHistoryConfiguration : IEntityTypeConfiguration<CourierLocationHistory>
{
    public void Configure(EntityTypeBuilder<CourierLocationHistory> builder)
    {
        builder.ToTable("courier_location_histories");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();
        builder.Property(l => l.CreatedBy).HasMaxLength(100);
        builder.Property(l => l.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(l => new { l.DeliveryId, l.RecordedAt });
        builder.HasIndex(l => new { l.CourierId, l.RecordedAt });

        builder.HasOne(l => l.Courier)
            .WithMany()
            .HasForeignKey(l => l.CourierId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
