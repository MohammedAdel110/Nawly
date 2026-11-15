using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nawly.Domain.Models.Platform;

namespace Nawly.Persistence.Configurations.Platform;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();
        builder.Property(l => l.Role).IsRequired().HasMaxLength(60);
        builder.Property(l => l.Action).IsRequired().HasMaxLength(120);
        builder.Property(l => l.EntityType).IsRequired().HasMaxLength(120);
        builder.Property(l => l.EntityId).IsRequired().HasMaxLength(100);
        builder.Property(l => l.OldValuesJson).HasColumnType("jsonb");
        builder.Property(l => l.NewValuesJson).HasColumnType("jsonb");
        builder.Property(l => l.IpAddress).HasMaxLength(64);
        builder.Property(l => l.UserAgent).HasMaxLength(300);
        builder.Property(l => l.CorrelationId).HasMaxLength(100);
        builder.Property(l => l.CreatedBy).HasMaxLength(100);
        builder.Property(l => l.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(l => new { l.EntityType, l.EntityId, l.At });
        builder.HasIndex(l => new { l.UserId, l.At });
    }
}

public sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("idempotency_records");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.RequestHash).IsRequired().HasMaxLength(128);
        builder.Property(r => r.ResponseBody).IsRequired();
        builder.Property(r => r.CreatedBy).HasMaxLength(100);
        builder.Property(r => r.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(r => new { r.UserId, r.Key }).IsUnique();
        builder.HasIndex(r => r.ExpiresAt);
    }
}

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.Type).IsRequired().HasMaxLength(150);
        builder.Property(m => m.PayloadJson).IsRequired().HasColumnType("jsonb");
        builder.Property(m => m.Error).HasMaxLength(2000);
        builder.Property(m => m.CreatedBy).HasMaxLength(100);
        builder.Property(m => m.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(m => new { m.ProcessedAt, m.LockedUntil, m.OccurredAt })
            .HasFilter("processed_at IS NULL");
    }
}

public sealed class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("system_settings");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Key).IsRequired().HasMaxLength(150);
        builder.Property(s => s.Value).IsRequired().HasMaxLength(2000);
        builder.Property(s => s.Type).IsRequired().HasMaxLength(40);
        builder.Property(s => s.Description).HasMaxLength(500);
        builder.Property(s => s.CreatedBy).HasMaxLength(100);
        builder.Property(s => s.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(s => s.Key).IsUnique();
    }
}
