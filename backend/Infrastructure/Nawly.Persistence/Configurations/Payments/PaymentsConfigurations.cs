using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nawly.Domain.Models.Payments;

namespace Nawly.Persistence.Configurations.Payments;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.ProviderOrderId).HasMaxLength(150);
        builder.Property(p => p.ProviderTransactionId).HasMaxLength(150);
        builder.Property(p => p.Amount).HasPrecision(12, 2);
        builder.Property(p => p.Currency).IsRequired().HasMaxLength(10);
        builder.Property(p => p.FailureCode).HasMaxLength(100);
        builder.Property(p => p.RowVersion).IsRowVersion();
        builder.Property(p => p.CreatedBy).HasMaxLength(100);
        builder.Property(p => p.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(p => new { p.OrderId, p.IdempotencyKey }).IsUnique();
        builder.HasIndex(p => p.ProviderOrderId);

        builder.HasOne(p => p.Order)
            .WithMany()
            .HasForeignKey(p => p.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PaymentWebhookEventConfiguration : IEntityTypeConfiguration<PaymentWebhookEvent>
{
    public void Configure(EntityTypeBuilder<PaymentWebhookEvent> builder)
    {
        builder.ToTable("payment_webhook_events");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.ExternalEventId).IsRequired().HasMaxLength(150);
        builder.Property(e => e.PayloadHash).IsRequired().HasMaxLength(128);
        builder.Property(e => e.Signature).IsRequired().HasMaxLength(300);
        builder.Property(e => e.Result).HasMaxLength(100);
        builder.Property(e => e.Error).HasMaxLength(500);
        builder.Property(e => e.CreatedBy).HasMaxLength(100);
        builder.Property(e => e.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(e => new { e.Provider, e.ExternalEventId }).IsUnique();
    }
}

public sealed class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.ToTable("refunds");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.Amount).HasPrecision(12, 2);
        builder.Property(r => r.ReasonCode).IsRequired().HasMaxLength(100);
        builder.Property(r => r.ProviderRefundId).HasMaxLength(150);
        builder.Property(r => r.CreatedBy).HasMaxLength(100);
        builder.Property(r => r.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(r => r.PaymentId);

        builder.HasOne(r => r.Payment)
            .WithMany(p => p.Refunds)
            .HasForeignKey(r => r.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
