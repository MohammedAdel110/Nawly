using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nawly.Domain.Models.Finance;

namespace Nawly.Persistence.Configurations.Finance;

public sealed class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.ToTable("ledger_entries");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Amount).HasPrecision(12, 2);
        builder.Property(e => e.Description).IsRequired().HasMaxLength(400);
        builder.Property(e => e.CreatedBy).HasMaxLength(100);
        builder.Property(e => e.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(e => new { e.AccountType, e.AccountId, e.SettlementId, e.CreatedAt });
        builder.HasIndex(e => e.OrderId);

        builder.HasOne(e => e.Settlement)
            .WithMany(s => s.LedgerEntries)
            .HasForeignKey(e => e.SettlementId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class SettlementConfiguration : IEntityTypeConfiguration<Settlement>
{
    public void Configure(EntityTypeBuilder<Settlement> builder)
    {
        builder.ToTable("settlements");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.GrossAmount).HasPrecision(12, 2);
        builder.Property(s => s.CommissionAmount).HasPrecision(12, 2);
        builder.Property(s => s.AdjustmentsAmount).HasPrecision(12, 2);
        builder.Property(s => s.NetAmount).HasPrecision(12, 2);
        builder.Property(s => s.PaymentReference).HasMaxLength(150);
        builder.Property(s => s.RowVersion).IsRowVersion();
        builder.Property(s => s.CreatedBy).HasMaxLength(100);
        builder.Property(s => s.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(s => new { s.PartyType, s.PartyId, s.Status, s.PeriodStart });
    }
}

public sealed class SettlementLineConfiguration : IEntityTypeConfiguration<SettlementLine>
{
    public void Configure(EntityTypeBuilder<SettlementLine> builder)
    {
        builder.ToTable("settlement_lines");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();
        builder.Property(l => l.Amount).HasPrecision(12, 2);
        builder.Property(l => l.CreatedBy).HasMaxLength(100);
        builder.Property(l => l.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(l => l.SettlementId);

        builder.HasOne(l => l.Settlement)
            .WithMany(s => s.Lines)
            .HasForeignKey(l => l.SettlementId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.LedgerEntry)
            .WithMany()
            .HasForeignKey(l => l.LedgerEntryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
