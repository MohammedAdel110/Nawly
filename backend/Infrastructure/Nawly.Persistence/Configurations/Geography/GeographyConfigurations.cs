using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nawly.Domain.Models.Geography;

namespace Nawly.Persistence.Configurations.Geography;

public sealed class DeliveryZoneConfiguration : IEntityTypeConfiguration<DeliveryZone>
{
    public void Configure(EntityTypeBuilder<DeliveryZone> builder)
    {
        builder.ToTable("delivery_zones");
        builder.HasKey(z => z.Id);
        builder.Property(z => z.Id).ValueGeneratedNever();
        builder.Property(z => z.Name).IsRequired().HasMaxLength(150);
        builder.Property(z => z.NameAr).IsRequired().HasMaxLength(150);
        builder.Property(z => z.BaseFee).HasPrecision(12, 2);
        builder.Property(z => z.PerKmFee).HasPrecision(12, 2);
        builder.Property(z => z.MinOrderAmount).HasPrecision(12, 2);
        builder.Property(z => z.FreeDeliveryThreshold).HasPrecision(12, 2);
        builder.Property(z => z.PolygonGeoJson).HasColumnType("jsonb");
        builder.Property(z => z.CreatedBy).HasMaxLength(100);
        builder.Property(z => z.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(z => z.IsActive);
    }
}

public sealed class AreaConfiguration : IEntityTypeConfiguration<Area>
{
    public void Configure(EntityTypeBuilder<Area> builder)
    {
        builder.ToTable("areas");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Name).IsRequired().HasMaxLength(150);
        builder.Property(a => a.NameAr).IsRequired().HasMaxLength(150);
        builder.Property(a => a.CreatedBy).HasMaxLength(100);
        builder.Property(a => a.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(a => new { a.ZoneId, a.IsActive, a.SortOrder });

        builder.HasOne(a => a.Zone)
            .WithMany(z => z.Areas)
            .HasForeignKey(a => a.ZoneId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("addresses");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Label).IsRequired().HasMaxLength(80);
        builder.Property(a => a.Description).IsRequired().HasMaxLength(500);
        builder.Property(a => a.Building).HasMaxLength(50);
        builder.Property(a => a.Floor).HasMaxLength(20);
        builder.Property(a => a.Apartment).HasMaxLength(20);
        builder.Property(a => a.ContactPhone).IsRequired().HasMaxLength(20);
        builder.Property(a => a.Notes).HasMaxLength(300);
        builder.Property(a => a.CreatedBy).HasMaxLength(100);
        builder.Property(a => a.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(a => new { a.CustomerId, a.IsDefault });

        builder.HasOne(a => a.Customer)
            .WithMany(c => c.Addresses)
            .HasForeignKey(a => a.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Area)
            .WithMany()
            .HasForeignKey(a => a.AreaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
