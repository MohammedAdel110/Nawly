using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nawly.Domain.Models.Identity;

namespace Nawly.Persistence.Configurations.Identity;

public sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.Property(u => u.FullName).IsRequired().HasMaxLength(150);
        builder.Property(u => u.PhoneNumber).HasMaxLength(20);
        builder.Property(u => u.CreatedBy).HasMaxLength(100);
        builder.Property(u => u.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(u => u.PhoneNumber)
            .IsUnique()
            .HasFilter("phone_number IS NOT NULL");

        builder.HasOne(u => u.CustomerProfile)
            .WithOne(p => p.User)
            .HasForeignKey<CustomerProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(u => u.CourierProfile)
            .WithOne(p => p.User)
            .HasForeignKey<CourierProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class AppRoleConfiguration : IEntityTypeConfiguration<AppRole>
{
    public void Configure(EntityTypeBuilder<AppRole> builder)
    {
        builder.Property(r => r.Description).HasMaxLength(300);
    }
}

public sealed class CustomerProfileConfiguration : IEntityTypeConfiguration<CustomerProfile>
{
    public void Configure(EntityTypeBuilder<CustomerProfile> builder)
    {
        builder.ToTable("customer_profiles");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.CreatedBy).HasMaxLength(100);
        builder.Property(c => c.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(c => c.UserId).IsUnique();
    }
}

public sealed class CourierProfileConfiguration : IEntityTypeConfiguration<CourierProfile>
{
    public void Configure(EntityTypeBuilder<CourierProfile> builder)
    {
        builder.ToTable("courier_profiles");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.NationalIdImageRef).IsRequired().HasMaxLength(300);
        builder.Property(c => c.PhotoRef).HasMaxLength(300);
        builder.Property(c => c.CashOnHand).HasPrecision(12, 2);
        builder.Property(c => c.RatingAvg).HasPrecision(5, 2);
        builder.Property(c => c.RejectionReason).HasMaxLength(500);
        builder.Property(c => c.CreatedBy).HasMaxLength(100);
        builder.Property(c => c.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(c => c.UserId).IsUnique();

        // Partial index targeting only online village couriers for sub-millisecond dispatch queries.
        builder.HasIndex(c => new { c.IsOnline, c.Status, c.LastLocationAt })
            .HasFilter("is_online = true AND is_deleted = false");
    }
}

public sealed class OtpChallengeConfiguration : IEntityTypeConfiguration<OtpChallenge>
{
    public void Configure(EntityTypeBuilder<OtpChallenge> builder)
    {
        builder.ToTable("otp_challenges");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();
        builder.Property(o => o.PhoneNumber).IsRequired().HasMaxLength(20);
        builder.Property(o => o.CodeHash).IsRequired().HasMaxLength(128);
        builder.Property(o => o.RequestIp).HasMaxLength(64);
        builder.Property(o => o.CreatedBy).HasMaxLength(100);
        builder.Property(o => o.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(o => new { o.PhoneNumber, o.Purpose, o.ExpiresAt });
    }
}

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.TokenHash).IsRequired().HasMaxLength(128);
        builder.Property(r => r.CreatedByIp).HasMaxLength(64);
        builder.Property(r => r.UserAgent).HasMaxLength(300);
        builder.Property(r => r.DeviceName).HasMaxLength(150);
        builder.Property(r => r.RevokedReason).HasMaxLength(100);
        builder.Property(r => r.CreatedBy).HasMaxLength(100);
        builder.Property(r => r.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(r => r.TokenHash).IsUnique();
        builder.HasIndex(r => r.FamilyId);
        builder.HasIndex(r => new { r.UserId, r.RevokedAt });

        builder.HasOne(r => r.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
