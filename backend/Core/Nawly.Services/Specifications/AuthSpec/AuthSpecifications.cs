using Nawly.Domain.Contracts.SpecificationPattern.BaseSpec;
using Nawly.Domain.Models.Catalog;
using Nawly.Domain.Models.Identity;
using Nawly.Shared.EnumHelper.UserEnums;

namespace Nawly.Services.Specifications.AuthSpec;

public sealed class LatestOtpChallengeSpecification : BaseSpecifications<OtpChallenge, Guid>
{
    public LatestOtpChallengeSpecification(string phoneNumber, OtpPurpose purpose)
        : base(o => !o.IsDeleted && o.PhoneNumber == phoneNumber && o.Purpose == purpose)
    {
        AddOrderBy(o => o.CreatedAt, isDescending: true);
    }
}

public sealed class UserByPhoneOrEmailSpecification : BaseSpecifications<AppUser, Guid>
{
    public UserByPhoneOrEmailSpecification(string identifier)
        : base(u => u.PhoneNumber == identifier || u.Email == identifier || u.UserName == identifier)
    {
    }
}

public sealed class ActiveStoreStaffByUserIdSpecification : BaseSpecifications<StoreStaff, Guid>
{
    public ActiveStoreStaffByUserIdSpecification(Guid userId)
        : base(s => s.UserId == userId && s.IsActive && !s.IsDeleted)
    {
        ApplyAsNoTracking();
    }
}

public sealed class RefreshTokenByHashSpecification : BaseSpecifications<RefreshToken, Guid>
{
    public RefreshTokenByHashSpecification(string tokenHash)
        : base(r => r.TokenHash == tokenHash && !r.IsDeleted)
    {
        AddInclude(r => r.User);
    }
}

public sealed class ActiveRefreshTokensByUserIdSpecification : BaseSpecifications<RefreshToken, Guid>
{
    public ActiveRefreshTokensByUserIdSpecification(Guid userId, DateTimeOffset utcNow)
        : base(r => r.UserId == userId && !r.IsDeleted && r.RevokedAt == null && r.ExpiresAt > utcNow)
    {
        AddOrderBy(r => r.CreatedAt, isDescending: true);
    }
}

public sealed class RefreshTokensByFamilyIdSpecification : BaseSpecifications<RefreshToken, Guid>
{
    public RefreshTokensByFamilyIdSpecification(Guid familyId)
        : base(r => r.FamilyId == familyId && !r.IsDeleted && r.RevokedAt == null)
    {
    }
}

public sealed class UserSessionByIdSpecification : BaseSpecifications<RefreshToken, Guid>
{
    public UserSessionByIdSpecification(Guid sessionId, Guid userId)
        : base(r => r.Id == sessionId && r.UserId == userId && !r.IsDeleted)
    {
    }
}
