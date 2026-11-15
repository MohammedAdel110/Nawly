using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nawly.Domain.Models.Catalog;

namespace Nawly.Persistence.Configurations.Catalog;

public sealed class StoreCategoryConfiguration : IEntityTypeConfiguration<StoreCategory>
{
    public void Configure(EntityTypeBuilder<StoreCategory> builder)
    {
        builder.ToTable("store_categories");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Name).IsRequired().HasMaxLength(120);
        builder.Property(c => c.NameAr).IsRequired().HasMaxLength(120);
        builder.Property(c => c.IconRef).HasMaxLength(300);
        builder.Property(c => c.CreatedBy).HasMaxLength(100);
        builder.Property(c => c.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(c => new { c.IsActive, c.SortOrder });
    }
}

public sealed class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.ToTable("stores");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Slug).IsRequired().HasMaxLength(160);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(150);
        builder.Property(s => s.NameAr).IsRequired().HasMaxLength(150);
        builder.Property(s => s.NormalizedSearchText).IsRequired().HasMaxLength(2500);
        builder.Property(s => s.Description).HasMaxLength(1000);
        builder.Property(s => s.DescriptionAr).HasMaxLength(1000);
        builder.Property(s => s.LogoRef).HasMaxLength(300);
        builder.Property(s => s.CoverRef).HasMaxLength(300);
        builder.Property(s => s.Phone).IsRequired().HasMaxLength(20);
        builder.Property(s => s.AddressText).IsRequired().HasMaxLength(400);
        builder.Property(s => s.MinOrderAmount).HasPrecision(12, 2);
        builder.Property(s => s.CommissionRate).HasPrecision(5, 2);
        builder.Property(s => s.RatingAvg).HasPrecision(5, 2);
        builder.Property(s => s.RejectionReason).HasMaxLength(500);
        builder.Property(s => s.RowVersion).IsRowVersion();
        builder.Property(s => s.CreatedBy).HasMaxLength(100);
        builder.Property(s => s.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(s => s.Slug).IsUnique();
        builder.HasIndex(s => new { s.Status, s.CategoryId, s.AreaId });

        builder.HasIndex(s => s.NormalizedSearchText)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        builder.HasOne(s => s.Category)
            .WithMany(c => c.Stores)
            .HasForeignKey(s => s.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Area)
            .WithMany()
            .HasForeignKey(s => s.AreaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class StoreDeliveryZoneConfiguration : IEntityTypeConfiguration<StoreDeliveryZone>
{
    public void Configure(EntityTypeBuilder<StoreDeliveryZone> builder)
    {
        builder.ToTable("store_delivery_zones");
        builder.HasKey(z => z.Id);
        builder.Property(z => z.Id).ValueGeneratedNever();
        builder.Property(z => z.FeeOverride).HasPrecision(12, 2);
        builder.Property(z => z.MinOrderOverride).HasPrecision(12, 2);
        builder.Property(z => z.CreatedBy).HasMaxLength(100);
        builder.Property(z => z.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(z => new { z.StoreId, z.ZoneId })
            .IsUnique()
            .HasFilter("is_deleted = false");

        builder.HasOne(z => z.Store)
            .WithMany(s => s.DeliveryZones)
            .HasForeignKey(z => z.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(z => z.Zone)
            .WithMany(dz => dz.StoreDeliveryZones)
            .HasForeignKey(z => z.ZoneId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class StoreOpeningHourConfiguration : IEntityTypeConfiguration<StoreOpeningHour>
{
    public void Configure(EntityTypeBuilder<StoreOpeningHour> builder)
    {
        builder.ToTable("store_opening_hours");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).ValueGeneratedNever();
        builder.Property(h => h.CreatedBy).HasMaxLength(100);
        builder.Property(h => h.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(h => new { h.StoreId, h.DayOfWeek });

        builder.HasOne(h => h.Store)
            .WithMany(s => s.OpeningHours)
            .HasForeignKey(h => h.StoreId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class StoreStaffConfiguration : IEntityTypeConfiguration<StoreStaff>
{
    public void Configure(EntityTypeBuilder<StoreStaff> builder)
    {
        builder.ToTable("store_staff");
        builder.HasKey(st => st.Id);
        builder.Property(st => st.Id).ValueGeneratedNever();
        builder.Property(st => st.CreatedBy).HasMaxLength(100);
        builder.Property(st => st.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(st => new { st.StoreId, st.UserId })
            .IsUnique()
            .HasFilter("is_deleted = false");

        builder.HasOne(st => st.Store)
            .WithMany(s => s.Staff)
            .HasForeignKey(st => st.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(st => st.User)
            .WithMany(u => u.StoreStaffAssignments)
            .HasForeignKey(st => st.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class MenuCategoryConfiguration : IEntityTypeConfiguration<MenuCategory>
{
    public void Configure(EntityTypeBuilder<MenuCategory> builder)
    {
        builder.ToTable("menu_categories");
        builder.HasKey(mc => mc.Id);
        builder.Property(mc => mc.Id).ValueGeneratedNever();
        builder.Property(mc => mc.Name).IsRequired().HasMaxLength(120);
        builder.Property(mc => mc.NameAr).IsRequired().HasMaxLength(120);
        builder.Property(mc => mc.CreatedBy).HasMaxLength(100);
        builder.Property(mc => mc.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(mc => new { mc.StoreId, mc.SortOrder });

        builder.HasOne(mc => mc.Store)
            .WithMany(s => s.MenuCategories)
            .HasForeignKey(mc => mc.StoreId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Name).IsRequired().HasMaxLength(180);
        builder.Property(p => p.NameAr).IsRequired().HasMaxLength(180);
        builder.Property(p => p.NormalizedSearchText).IsRequired().HasMaxLength(2500);
        builder.Property(p => p.Description).HasMaxLength(1000);
        builder.Property(p => p.DescriptionAr).HasMaxLength(1000);
        builder.Property(p => p.ImageRef).HasMaxLength(300);
        builder.Property(p => p.Price).HasPrecision(12, 2);
        builder.Property(p => p.CompareAtPrice).HasPrecision(12, 2);
        builder.Property(p => p.Sku).HasMaxLength(80);
        builder.Property(p => p.Barcode).HasMaxLength(80);
        builder.Property(p => p.RowVersion).IsRowVersion();
        builder.Property(p => p.CreatedBy).HasMaxLength(100);
        builder.Property(p => p.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(p => new { p.StoreId, p.MenuCategoryId, p.IsAvailable, p.SortOrder });

        builder.HasIndex(p => p.NormalizedSearchText)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        builder.HasOne(p => p.Store)
            .WithMany(s => s.Products)
            .HasForeignKey(p => p.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.MenuCategory)
            .WithMany(mc => mc.Products)
            .HasForeignKey(p => p.MenuCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProductOptionGroupConfiguration : IEntityTypeConfiguration<ProductOptionGroup>
{
    public void Configure(EntityTypeBuilder<ProductOptionGroup> builder)
    {
        builder.ToTable("product_option_groups");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).ValueGeneratedNever();
        builder.Property(g => g.Name).IsRequired().HasMaxLength(120);
        builder.Property(g => g.NameAr).IsRequired().HasMaxLength(120);
        builder.Property(g => g.CreatedBy).HasMaxLength(100);
        builder.Property(g => g.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(g => new { g.ProductId, g.SortOrder });

        builder.HasOne(g => g.Product)
            .WithMany(p => p.OptionGroups)
            .HasForeignKey(g => g.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ProductOptionConfiguration : IEntityTypeConfiguration<ProductOption>
{
    public void Configure(EntityTypeBuilder<ProductOption> builder)
    {
        builder.ToTable("product_options");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();
        builder.Property(o => o.Name).IsRequired().HasMaxLength(120);
        builder.Property(o => o.NameAr).IsRequired().HasMaxLength(120);
        builder.Property(o => o.PriceDelta).HasPrecision(12, 2);
        builder.Property(o => o.CreatedBy).HasMaxLength(100);
        builder.Property(o => o.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(o => new { o.GroupId, o.SortOrder });

        builder.HasOne(o => o.Group)
            .WithMany(g => g.Options)
            .HasForeignKey(o => o.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
