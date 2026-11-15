using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nawly.Domain.Models.Engagement;

namespace Nawly.Persistence.Configurations.Engagement;

public sealed class BannerConfiguration : IEntityTypeConfiguration<Banner>
{
    public void Configure(EntityTypeBuilder<Banner> builder)
    {
        builder.ToTable("banners");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();
        builder.Property(b => b.Title).IsRequired().HasMaxLength(150);
        builder.Property(b => b.TitleAr).IsRequired().HasMaxLength(150);
        builder.Property(b => b.ImageRef).IsRequired().HasMaxLength(300);
        builder.Property(b => b.TargetUrl).HasMaxLength(500);
        builder.Property(b => b.CreatedBy).HasMaxLength(100);
        builder.Property(b => b.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(b => new { b.IsActive, b.StartsAt, b.EndsAt, b.SortOrder });
    }
}

public sealed class FavoriteConfiguration : IEntityTypeConfiguration<Favorite>
{
    public void Configure(EntityTypeBuilder<Favorite> builder)
    {
        builder.ToTable("favorites");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();
        builder.Property(f => f.CreatedBy).HasMaxLength(100);
        builder.Property(f => f.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(f => new { f.CustomerId, f.StoreId })
            .IsUnique()
            .HasFilter("is_deleted = false");

        builder.HasOne(f => f.Customer)
            .WithMany()
            .HasForeignKey(f => f.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.Store)
            .WithMany()
            .HasForeignKey(f => f.StoreId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();
        builder.Property(n => n.Title).IsRequired().HasMaxLength(150);
        builder.Property(n => n.Body).IsRequired().HasMaxLength(1000);
        builder.Property(n => n.DataJson).HasColumnType("jsonb");
        builder.Property(n => n.CreatedBy).HasMaxLength(100);
        builder.Property(n => n.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(n => new { n.UserId, n.ReadAt, n.CreatedAt });

        builder.HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("reviews");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.Comment).HasMaxLength(1000);
        builder.Property(r => r.StoreReply).HasMaxLength(1000);
        builder.Property(r => r.CreatedBy).HasMaxLength(100);
        builder.Property(r => r.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(r => r.OrderId)
            .IsUnique()
            .HasFilter("is_deleted = false");
        builder.HasIndex(r => new { r.StoreId, r.Status, r.CreatedAt });

        builder.HasOne(r => r.Order)
            .WithMany()
            .HasForeignKey(r => r.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Customer)
            .WithMany()
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Store)
            .WithMany()
            .HasForeignKey(r => r.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Courier)
            .WithMany()
            .HasForeignKey(r => r.CourierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class SupportTicketConfiguration : IEntityTypeConfiguration<SupportTicket>
{
    public void Configure(EntityTypeBuilder<SupportTicket> builder)
    {
        builder.ToTable("support_tickets");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.Subject).IsRequired().HasMaxLength(200);
        builder.Property(t => t.CreatedBy).HasMaxLength(100);
        builder.Property(t => t.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(t => new { t.UserId, t.Status });
        builder.HasIndex(t => new { t.Status, t.CreatedAt });

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class SupportMessageConfiguration : IEntityTypeConfiguration<SupportMessage>
{
    public void Configure(EntityTypeBuilder<SupportMessage> builder)
    {
        builder.ToTable("support_messages");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.Body).IsRequired().HasMaxLength(2000);
        builder.Property(m => m.AttachmentRef).HasMaxLength(300);
        builder.Property(m => m.CreatedBy).HasMaxLength(100);
        builder.Property(m => m.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(m => new { m.TicketId, m.CreatedAt });

        builder.HasOne(m => m.Ticket)
            .WithMany(t => t.Messages)
            .HasForeignKey(m => m.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class WebPushSubscriptionConfiguration : IEntityTypeConfiguration<WebPushSubscription>
{
    public void Configure(EntityTypeBuilder<WebPushSubscription> builder)
    {
        builder.ToTable("web_push_subscriptions");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).ValueGeneratedNever();
        builder.Property(w => w.Endpoint).IsRequired().HasMaxLength(500);
        builder.Property(w => w.P256dh).IsRequired().HasMaxLength(300);
        builder.Property(w => w.Auth).IsRequired().HasMaxLength(200);
        builder.Property(w => w.UserAgent).HasMaxLength(300);
        builder.Property(w => w.CreatedBy).HasMaxLength(100);
        builder.Property(w => w.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(w => new { w.UserId, w.Endpoint }).IsUnique();

        builder.HasOne(w => w.User)
            .WithMany()
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
