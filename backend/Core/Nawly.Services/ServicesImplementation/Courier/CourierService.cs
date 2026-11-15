using Nawly.Abstraction.IServices.Auth;
using Nawly.Abstraction.IServices.Courier;
using Nawly.Abstraction.IServices.Notification;
using Nawly.Domain.Contracts.SpecificReposPattern;
using Nawly.Domain.Contracts.UnitOfWorkPattern;
using Nawly.Domain.Models.Delivery;
using Nawly.Domain.Models.Identity;
using Nawly.Domain.ValueObjects;
using Nawly.Services.Mapping.CourierMapping;
using Nawly.Services.Specifications.CourierSpec;
using Nawly.Services.Specifications.FinanceSpec;
using Nawly.Services.Specifications.PromotionSpec;
using Nawly.Shared.Common.Pagination;
using Nawly.Shared.Common.Params.Courier;
using Nawly.Shared.Common.Results;
using Nawly.Shared.DTOs.Courier;
using Nawly.Shared.EnumHelper.DeliveryEnums;
using Nawly.Shared.EnumHelper.FinanceEnums;

namespace Nawly.Services.ServicesImplementation.Courier;

public sealed class CourierService(
    IUnitOfWork unitOfWork,
    IDeliveryRepository deliveryRepository,
    IRealtimeNotifier realtimeNotifier,
    IClock clock) : ICourierService
{
    public async Task<Result<CourierProfileDto>> ApplyAsync(ApplyCourierDto request, CancellationToken ct = default)
    {
        if (!PhoneNumber.TryParseEgyptianMobile(request.PhoneNumber, out var phone) || phone is null)
            return Result.Failure<CourierProfileDto>(Error.Validation("courier.invalid_phone", "Phone number must be a valid Egyptian mobile number."));

        var userRepo = unitOfWork.GetRepository<AppUser, Guid>();
        var existingUser = await userRepo.GetByIdWithSpecAsync(new UserByPhoneSpecification(phone.Value), ct);
        if (existingUser is not null)
            return Result.Failure<CourierProfileDto>(Error.Conflict("courier.phone_exists", "An account already exists with this phone number."));

        var user = new AppUser
        {
            FullName = request.FullName.Trim(),
            PhoneNumber = phone.Value,
            PhoneNumberConfirmed = true,
            UserName = phone.Value,
            NormalizedUserName = phone.Value.ToUpperInvariant(),
            SecurityStamp = Guid.NewGuid().ToString("N"),
            CreatedAt = clock.UtcNow
        };
        user.PasswordHash = ServiceHelpers.HashPassword(user, request.Password);

        var profile = new CourierProfile
        {
            UserId = user.Id,
            User = user,
            Status = CourierStatus.Pending,
            VehicleType = request.VehicleType,
            NationalIdImageRef = request.NationalIdImageRef,
            PhotoRef = request.PhotoRef,
            CreatedAt = clock.UtcNow
        };

        await userRepo.AddAsync(user, ct);
        await unitOfWork.GetRepository<CourierProfile, Guid>().AddAsync(profile, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(profile.ToDto());
    }

    public async Task<Result<CourierProfileDto>> GetProfileByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var profile = await unitOfWork
            .GetRepository<CourierProfile, Guid>()
            .GetByIdWithSpecAsync(new CourierByUserIdSpecification(userId, asNoTracking: true), ct);

        if (profile is null)
            return Result.Failure<CourierProfileDto>(Error.NotFound("courier.not_found", "Courier profile not found."));

        return Result.Success(profile.ToDto());
    }

    public async Task<Result<CourierProfileDto>> ToggleOnlineAsync(
        Guid userId,
        ToggleCourierOnlineDto request,
        CancellationToken ct = default)
    {
        var repo = unitOfWork.GetRepository<CourierProfile, Guid>();
        var profile = await repo.GetByIdWithSpecAsync(new CourierByUserIdSpecification(userId), ct);
        if (profile is null)
            return Result.Failure<CourierProfileDto>(Error.NotFound("courier.not_found", "Courier profile not found."));

        if (profile.Status != CourierStatus.Approved)
            return Result.Failure<CourierProfileDto>(Error.Forbidden("courier.not_approved", "Only approved couriers can go online."));

        var shiftRepo = unitOfWork.GetRepository<CourierShift, Guid>();
        var activeShift = await shiftRepo.GetByIdWithSpecAsync(new ActiveCourierShiftSpecification(profile.Id), ct);

        if (request.IsOnline && !profile.IsOnline)
        {
            profile.IsOnline = true;
            if (activeShift is null)
            {
                await shiftRepo.AddAsync(new CourierShift
                {
                    CourierId = profile.Id,
                    StartedAt = clock.UtcNow,
                    CreatedAt = clock.UtcNow
                }, ct);
            }
        }
        else if (!request.IsOnline && profile.IsOnline)
        {
            profile.IsOnline = false;
            if (activeShift is not null)
            {
                activeShift.End("courier_offline", clock.UtcNow);
                shiftRepo.Update(activeShift);
            }
        }

        profile.UpdatedAt = clock.UtcNow;
        repo.Update(profile);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(profile.ToDto());
    }

    public async Task<Result> UpdateLocationAsync(Guid userId, CourierLocationUpdateDto request, CancellationToken ct = default)
    {
        var repo = unitOfWork.GetRepository<CourierProfile, Guid>();
        var profile = await repo.GetByIdWithSpecAsync(new CourierByUserIdSpecification(userId), ct);
        if (profile is null)
            return Result.Failure(Error.NotFound("courier.not_found", "Courier profile not found."));

        var timestamp = request.RecordedAt ?? clock.UtcNow;
        profile.UpdateLocation(request.Latitude, request.Longitude, timestamp);
        profile.UpdatedAt = clock.UtcNow;
        repo.Update(profile);

        var activeDeliveries = await deliveryRepository.GetActiveByCourierIdAsync(profile.Id, ct);
        var activeDeliveryId = activeDeliveries.FirstOrDefault()?.Id;

        await unitOfWork.GetRepository<CourierLocationHistory, Guid>().AddAsync(new CourierLocationHistory
        {
            CourierId = profile.Id,
            DeliveryId = activeDeliveryId,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            RecordedAt = timestamp,
            CreatedAt = clock.UtcNow
        }, ct);

        await unitOfWork.SaveChangesAsync(ct);

        foreach (var delivery in activeDeliveries)
            await realtimeNotifier.NotifyCourierLocationAsync(delivery.OrderId, request.Latitude, request.Longitude, timestamp, ct);

        return Result.Success();
    }

    public async Task<Result<CourierEarningsDto>> GetEarningsAsync(
        Guid userId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        CancellationToken ct = default)
    {
        var profile = await unitOfWork
            .GetRepository<CourierProfile, Guid>()
            .GetByIdWithSpecAsync(new CourierByUserIdSpecification(userId, asNoTracking: true), ct);

        if (profile is null)
            return Result.Failure<CourierEarningsDto>(Error.NotFound("courier.not_found", "Courier profile not found."));

        var deliveries = await unitOfWork
            .GetRepository<Domain.Models.Delivery.Delivery, Guid>()
            .GetAllWithSpecAsync(new CourierCompletedDeliveriesSpecification(profile.Id, periodStart, periodEnd), ct);

        var totalPayouts = decimal.Round(deliveries.Sum(d => d.CourierPayout), 2, MidpointRounding.AwayFromZero);

        var unsettledEntries = await unitOfWork
            .GetRepository<Domain.Models.Finance.LedgerEntry, Guid>()
            .GetAllWithSpecAsync(new LedgerEntriesByAccountSpecification(LedgerAccountType.Courier, profile.Id), ct);

        var pendingSettlement = decimal.Round(
            unsettledEntries
                .Where(e => e.SettlementId is null && e.Kind == LedgerEntryKind.CourierPayout)
                .Sum(e => e.Direction == LedgerDirection.Credit ? e.Amount : -e.Amount),
            2,
            MidpointRounding.AwayFromZero);

        return Result.Success(new CourierEarningsDto(
            deliveries.Count,
            totalPayouts,
            pendingSettlement,
            profile.CashOnHand,
            periodStart,
            periodEnd));
    }

    public async Task<Result<PaginationResponse<CourierProfileDto>>> SearchCouriersAsync(
        CourierQueryParams queryParams,
        CancellationToken ct = default)
    {
        var repo = unitOfWork.GetRepository<CourierProfile, Guid>();
        var items = await repo.GetAllWithSpecAsync(new CouriersFilterSpecification(queryParams), ct);
        var count = await repo.GetCountAsync(new CouriersCountSpecification(queryParams), ct);

        return Result.Success(new PaginationResponse<CourierProfileDto>(
            queryParams.PageIndex,
            queryParams.PageSize,
            count,
            items.Select(c => c.ToDto()).ToList()));
    }
}
