using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nawly.Domain.Models.Catalog;
using Nawly.Domain.Models.Ordering;
using Nawly.Domain.Models.Promotions;

namespace Nawly.Persistence.Configurations.Ordering;

public sealed class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("carts");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.RowVersion).IsRowVersion();
        builder.Property(c => c.CreatedBy).HasMaxLength(100);
        builder.Property(c => c.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(c => c.CustomerId)
            .IsUnique()
            .HasFilter("is_deleted = false");

        builder.HasOne(c => c.Customer)
            .WithMany()
            .HasForeignKey(c => c.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Store)
            .WithMany()
            .HasForeignKey(c => c.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("cart_items");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();
        builder.Property(i => i.Note).HasMaxLength(300);
        builder.Property(i => i.CreatedBy).HasMaxLength(100);
        builder.Property(i => i.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(i => new { i.CartId, i.ProductId });

        builder.HasOne(i => i.Cart)
            .WithMany(c => c.Items)
            .HasForeignKey(i => i.CartId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CartItemOptionConfiguration : IEntityTypeConfiguration<CartItemOption>
{
    public void Configure(EntityTypeBuilder<CartItemOption> builder)
    {
        builder.ToTable("cart_item_options");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();
        builder.Property(o => o.CreatedBy).HasMaxLength(100);
        builder.Property(o => o.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(o => new { o.CartItemId, o.ProductOptionId })
            .IsUnique()
            .HasFilter("is_deleted = false");

        builder.HasOne(o => o.CartItem)
            .WithMany(i => i.Options)
            .HasForeignKey(o => o.CartItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(o => o.ProductOption)
            .WithMany()
            .HasForeignKey(o => o.ProductOptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();
        builder.Property(o => o.OrderNumber).IsRequired().HasMaxLength(32);

        builder.Property(o => o.Subtotal).HasPrecision(12, 2);
        builder.Property(o => o.DeliveryFee).HasPrecision(12, 2);
        builder.Property(o => o.ServiceFee).HasPrecision(12, 2);
        builder.Property(o => o.TaxAmount).HasPrecision(12, 2);
        builder.Property(o => o.DiscountAmount).HasPrecision(12, 2);
        builder.Property(o => o.Total).HasPrecision(12, 2);
        builder.Property(o => o.CashRounding).HasPrecision(12, 2);
        builder.Property(o => o.CommissionRate).HasPrecision(5, 2);
        builder.Property(o => o.CommissionAmount).HasPrecision(12, 2);
        builder.Property(o => o.Currency).IsRequired().HasMaxLength(10);

        builder.Property(o => o.CustomerNote).HasMaxLength(500);
        builder.Property(o => o.CustomerName).IsRequired().HasMaxLength(150);
        builder.Property(o => o.CustomerPhone).IsRequired().HasMaxLength(20);
        builder.Property(o => o.AddressLabel).IsRequired().HasMaxLength(80);
        builder.Property(o => o.AreaName).IsRequired().HasMaxLength(150);
        builder.Property(o => o.AddressDescription).IsRequired().HasMaxLength(500);
        builder.Property(o => o.Building).HasMaxLength(50);
        builder.Property(o => o.Floor).HasMaxLength(20);
        builder.Property(o => o.Apartment).HasMaxLength(20);
        builder.Property(o => o.DeliveryNotes).HasMaxLength(300);
        builder.Property(o => o.CancelReasonText).HasMaxLength(500);
        builder.Property(o => o.DeliveryConfirmationCode).HasMaxLength(16);
        builder.Property(o => o.DeliveryConfirmationCodeHash).HasMaxLength(128);
        builder.Property(o => o.RowVersion).IsRowVersion();
        builder.Property(o => o.CreatedBy).HasMaxLength(100);
        builder.Property(o => o.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(o => o.OrderNumber).IsUnique();
        builder.HasIndex(o => new { o.CustomerId, o.IdempotencyKey }).IsUnique();
        builder.HasIndex(o => new { o.StoreId, o.Status, o.PlacedAt });
        builder.HasIndex(o => new { o.CustomerId, o.PlacedAt });
        builder.HasIndex(o => new { o.Status, o.PlacedAt })
            .HasFilter("status = 1 AND is_deleted = false");

        builder.HasOne(o => o.Customer)
            .WithMany()
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Store)
            .WithMany()
            .HasForeignKey(o => o.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Coupon>()
            .WithMany()
            .HasForeignKey(o => o.CouponId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(o => o.Items)
            .HasField("_items")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(o => o.StatusHistory)
            .WithOne()
            .HasForeignKey(h => h.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(o => o.StatusHistory)
            .HasField("_statusHistory")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();
        builder.Property(i => i.ProductName).IsRequired().HasMaxLength(180);
        builder.Property(i => i.ProductNameAr).IsRequired().HasMaxLength(180);
        builder.Property(i => i.UnitPrice).HasPrecision(12, 2);
        builder.Property(i => i.LineTotal).HasPrecision(12, 2);
        builder.Property(i => i.Note).HasMaxLength(300);
        builder.Property(i => i.CreatedBy).HasMaxLength(100);
        builder.Property(i => i.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(i => i.OrderId);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(i => i.Options)
            .WithOne()
            .HasForeignKey(o => o.OrderItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class OrderItemOptionConfiguration : IEntityTypeConfiguration<OrderItemOption>
{
    public void Configure(EntityTypeBuilder<OrderItemOption> builder)
    {
        builder.ToTable("order_item_options");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();
        builder.Property(o => o.GroupName).IsRequired().HasMaxLength(150);
        builder.Property(o => o.GroupNameAr).IsRequired().HasMaxLength(150);
        builder.Property(o => o.OptionName).IsRequired().HasMaxLength(150);
        builder.Property(o => o.OptionNameAr).IsRequired().HasMaxLength(150);
        builder.Property(o => o.PriceDelta).HasPrecision(12, 2);
        builder.Property(o => o.CreatedBy).HasMaxLength(100);
        builder.Property(o => o.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(o => o.OrderItemId);
    }
}

public sealed class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
    {
        builder.ToTable("order_status_histories");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).ValueGeneratedNever();
        builder.Property(h => h.ReasonText).HasMaxLength(500);
        builder.Property(h => h.CreatedBy).HasMaxLength(100);
        builder.Property(h => h.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(h => new { h.OrderId, h.At });
    }
}
