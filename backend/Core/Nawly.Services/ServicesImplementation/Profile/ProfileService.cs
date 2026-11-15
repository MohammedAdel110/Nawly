using Nawly.Abstraction.IServices.Auth;
using Nawly.Abstraction.IServices.Profile;
using Nawly.Domain.Contracts.UnitOfWorkPattern;
using Nawly.Domain.Models.Identity;
using Nawly.Services.Mapping.AddressMapping;
using Nawly.Services.Mapping.ProfileMapping;
using Nawly.Services.Specifications.OrderSpec;
using Nawly.Shared.Common.Results;
using Nawly.Shared.DTOs.Profile;
using Nawly.Shared.EnumHelper.UserEnums;
using AddressEntity = Nawly.Domain.Models.Geography.Address;
using OrderEntity = Nawly.Domain.Models.Ordering.Order;

namespace Nawly.Services.ServicesImplementation.Profile;

public sealed class ProfileService(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock) : IProfileService
{
    public async Task<Result<ProfileDto>> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await unitOfWork.GetRepository<AppUser, Guid>().GetByIdAsync(userId, ct);
        if (user is null || user.Status == UserStatus.Deleted)
            return Result.Failure<ProfileDto>(Error.NotFound("profile.not_found", "User profile not found."));

        var roles = currentUser.UserId == userId
            ? currentUser.Roles.ToList()
            : [];

        return Result.Success(user.ToProfileDto(roles));
    }

    public async Task<Result<ProfileDto>> UpdateProfileAsync(Guid userId, UpdateProfileDto request, CancellationToken ct = default)
    {
        var userRepo = unitOfWork.GetRepository<AppUser, Guid>();
        var user = await userRepo.GetByIdAsync(userId, ct);
        if (user is null || user.Status == UserStatus.Deleted)
            return Result.Failure<ProfileDto>(Error.NotFound("profile.not_found", "User profile not found."));

        user.FullName = request.FullName.Trim();
        user.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        user.NormalizedEmail = user.Email?.ToUpperInvariant();
        user.PreferredLanguage = request.PreferredLanguage;
        user.UpdatedAt = clock.UtcNow;

        userRepo.Update(user);
        await unitOfWork.SaveChangesAsync(ct);

        var roles = currentUser.UserId == userId
            ? currentUser.Roles.ToList()
            : [];

        return Result.Success(user.ToProfileDto(roles));
    }

    public async Task<Result<UserDataExportDto>> ExportUserDataAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await unitOfWork.GetRepository<AppUser, Guid>().GetByIdAsync(userId, ct);
        if (user is null || user.Status == UserStatus.Deleted)
            return Result.Failure<UserDataExportDto>(Error.NotFound("profile.not_found", "User profile not found."));

        var customer = await ServiceHelpers.ResolveCustomerProfileAsync(unitOfWork, userId, ct);
        var addresses = customer is null
            ? []
            : await unitOfWork.GetRepository<AddressEntity, Guid>().GetAllWithSpecAsync(new AddressesByCustomerSpecification(customer.Id), ct);

        var completedCount = customer is null
            ? 0
            : await unitOfWork.GetRepository<OrderEntity, Guid>().GetCountAsync(new CustomerCompletedOrdersCountSpecification(customer.Id), ct);

        var roles = currentUser.UserId == userId
            ? currentUser.Roles.ToList()
            : [];

        var export = new UserDataExportDto(
            user.ToProfileDto(roles),
            addresses.Select(a => a.ToDto()).ToList(),
            completedCount,
            clock.UtcNow);

        return Result.Success(export);
    }

    public async Task<Result> RequestAccountDeletionAsync(Guid userId, CancellationToken ct = default)
    {
        var userRepo = unitOfWork.GetRepository<AppUser, Guid>();
        var user = await userRepo.GetByIdAsync(userId, ct);
        if (user is null || user.Status == UserStatus.Deleted)
            return Result.Failure(Error.NotFound("profile.not_found", "User profile not found."));

        user.Status = UserStatus.PendingDeletion;
        user.UpdatedAt = clock.UtcNow;
        userRepo.Update(user);

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
