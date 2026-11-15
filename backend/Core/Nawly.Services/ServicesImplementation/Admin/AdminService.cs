using Nawly.Abstraction.IServices.Admin;
using Nawly.Abstraction.IServices.Auth;
using Nawly.Domain.Contracts.UnitOfWorkPattern;
using Nawly.Domain.Models.Engagement;
using Nawly.Domain.Models.Identity;
using Nawly.Domain.Models.Platform;
using Nawly.Services.Mapping.AuthMapping;
using Nawly.Services.Mapping.CourierMapping;
using Nawly.Services.Mapping.ProfileMapping;
using Nawly.Services.Mapping.StoreMapping;
using Nawly.Services.Specifications.AdminSpec;
using Nawly.Services.Specifications.OrderSpec;
using Nawly.Services.Specifications.StoreSpec;
using Nawly.Shared.Common.Pagination;
using Nawly.Shared.Common.Params.Admin;
using Nawly.Shared.Common.Results;
using Nawly.Shared.DTOs.Admin;
using Nawly.Shared.DTOs.Courier;
using Nawly.Shared.DTOs.Store;
using Nawly.Shared.EnumHelper.DeliveryEnums;
using Nawly.Shared.EnumHelper.OrderEnums;
using Nawly.Shared.EnumHelper.StoreEnums;
using Nawly.Shared.EnumHelper.UserEnums;
using OrderEntity = Nawly.Domain.Models.Ordering.Order;
using StoreEntity = Nawly.Domain.Models.Catalog.Store;

namespace Nawly.Services.ServicesImplementation.Admin;

public sealed class AdminService(
    IUnitOfWork unitOfWork,
    IClock clock) : IAdminService
{
    public async Task<Result<PlatformDashboardDto>> GetDashboardAsync(CancellationToken ct = default)
    {
        var cairoDate = clock.CairoDate;
        var todayStartUtc = new DateTimeOffset(cairoDate.Year, cairoDate.Month, cairoDate.Day, 0, 0, 0, clock.CairoNow.Offset).ToUniversalTime();

        var orderRepo = unitOfWork.GetRepository<OrderEntity, Guid>();
        var todayOrders = await orderRepo.GetAllWithSpecAsync(new CustomerOrdersCountSinceSpecification(null, todayStartUtc), ct);
        var todayGmv = decimal.Round(
            todayOrders.Where(o => o.Status is not (OrderStatus.Cancelled or OrderStatus.Rejected)).Sum(o => o.Total),
            2,
            MidpointRounding.AwayFromZero);

        var activeOrders = await orderRepo.GetCountAsync(new ActiveOrdersCountSpecification(), ct);
        var delayedOrders = await orderRepo.GetCountAsync(new DelayedOrdersCountSpecification(clock.UtcNow), ct);
        var pendingStores = await unitOfWork.GetRepository<StoreEntity, Guid>().GetCountAsync(new PendingStoresCountSpecification(), ct);
        var pendingCouriers = await unitOfWork.GetRepository<CourierProfile, Guid>().GetCountAsync(new PendingCouriersCountSpecification(), ct);
        var onlineCouriers = await unitOfWork.GetRepository<CourierProfile, Guid>().GetCountAsync(new OnlineCouriersCountSpecification(), ct);
        var openTickets = await unitOfWork.GetRepository<SupportTicket, Guid>().GetCountAsync(new OpenSupportTicketsCountSpecification(), ct);

        return Result.Success(new PlatformDashboardDto(
            todayOrders.Count,
            todayGmv,
            activeOrders,
            delayedOrders,
            pendingStores,
            pendingCouriers,
            onlineCouriers,
            openTickets));
    }

    public async Task<Result<PaginationResponse<AdminUserDto>>> GetUsersAsync(
        UserQueryParams queryParams,
        CancellationToken ct = default)
    {
        var repo = unitOfWork.GetRepository<AppUser, Guid>();
        var users = await repo.GetAllWithSpecAsync(new UsersFilterSpecification(queryParams), ct);
        var count = await repo.GetCountAsync(new UsersCountSpecification(queryParams), ct);

        var dtos = users.Select(u =>
        {
            var roles = InferRoles(u);
            return u.ToAdminUserDto(roles);
        }).ToList();

        return Result.Success(new PaginationResponse<AdminUserDto>(queryParams.PageIndex, queryParams.PageSize, count, dtos));
    }

    public async Task<Result> SuspendUserAsync(
        Guid adminUserId,
        Guid targetUserId,
        SuspendUserDto request,
        CancellationToken ct = default)
    {
        var repo = unitOfWork.GetRepository<AppUser, Guid>();
        var user = await repo.GetByIdAsync(targetUserId, ct);
        if (user is null)
            return Result.Failure(Error.NotFound("admin.user_not_found", "User not found."));

        var previous = user.Status;
        user.Status = UserStatus.Suspended;
        user.UpdatedAt = clock.UtcNow;
        user.UpdatedBy = adminUserId.ToString();
        repo.Update(user);

        await WriteAuditAsync(adminUserId, "SuspendUser", nameof(AppUser), user.Id.ToString(), previous.ToString(), request.Reason, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> ActivateUserAsync(Guid adminUserId, Guid targetUserId, CancellationToken ct = default)
    {
        var repo = unitOfWork.GetRepository<AppUser, Guid>();
        var user = await repo.GetByIdAsync(targetUserId, ct);
        if (user is null)
            return Result.Failure(Error.NotFound("admin.user_not_found", "User not found."));

        var previous = user.Status;
        user.Status = UserStatus.Active;
        user.UpdatedAt = clock.UtcNow;
        user.UpdatedBy = adminUserId.ToString();
        repo.Update(user);

        await WriteAuditAsync(adminUserId, "ActivateUser", nameof(AppUser), user.Id.ToString(), previous.ToString(), nameof(UserStatus.Active), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<StoreDetailsDto>> ReviewStoreApplicationAsync(
        Guid adminUserId,
        Guid storeId,
        ReviewApplicationDto request,
        CancellationToken ct = default)
    {
        var repo = unitOfWork.GetRepository<StoreEntity, Guid>();
        var store = await repo.GetByIdWithSpecAsync(new StoreWithDetailsSpecification(storeId, asNoTracking: false), ct);
        if (store is null)
            return Result.Failure<StoreDetailsDto>(Error.NotFound("store.not_found", "Store not found."));

        var previous = store.Status;
        if (request.Approved)
        {
            store.Status = StoreStatus.Active;
            store.ApprovedAt = clock.UtcNow;
            store.ApprovedBy = adminUserId;
            store.RejectionReason = null;
            if (request.CommissionRateOverride.HasValue)
                store.CommissionRate = Math.Clamp(request.CommissionRateOverride.Value, 0m, 1m);
        }
        else
        {
            store.Status = StoreStatus.Rejected;
            store.RejectionReason = request.RejectionReason?.Trim();
        }

        store.UpdatedAt = clock.UtcNow;
        store.UpdatedBy = adminUserId.ToString();
        repo.Update(store);

        await WriteAuditAsync(
            adminUserId,
            request.Approved ? "ApproveStore" : "RejectStore",
            nameof(StoreEntity),
            store.Id.ToString(),
            previous.ToString(),
            store.Status.ToString(),
            ct);

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(store.ToDetailsDto(clock.CairoLocalTime, clock.CairoDayOfWeek, clock.UtcNow));
    }

    public async Task<Result<CourierProfileDto>> ReviewCourierApplicationAsync(
        Guid adminUserId,
        Guid courierId,
        ReviewApplicationDto request,
        CancellationToken ct = default)
    {
        var repo = unitOfWork.GetRepository<CourierProfile, Guid>();
        var courier = await repo.GetByIdAsync(courierId, ct);
        if (courier is null || courier.IsDeleted)
            return Result.Failure<CourierProfileDto>(Error.NotFound("courier.not_found", "Courier profile not found."));

        var previous = courier.Status;
        if (request.Approved)
        {
            courier.Status = CourierStatus.Approved;
            courier.RejectionReason = null;
        }
        else
        {
            courier.Status = CourierStatus.Rejected;
            courier.RejectionReason = request.RejectionReason?.Trim();
        }

        courier.UpdatedAt = clock.UtcNow;
        courier.UpdatedBy = adminUserId.ToString();
        repo.Update(courier);

        await WriteAuditAsync(
            adminUserId,
            request.Approved ? "ApproveCourier" : "RejectCourier",
            nameof(CourierProfile),
            courier.Id.ToString(),
            previous.ToString(),
            courier.Status.ToString(),
            ct);

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(courier.ToDto());
    }

    public async Task<Result<IReadOnlyList<SystemSettingDto>>> GetSystemSettingsAsync(CancellationToken ct = default)
    {
        var settings = await unitOfWork
            .GetRepository<SystemSetting, Guid>()
            .GetAllWithSpecAsync(new SystemSettingsSpecification(), ct);

        return Result.Success<IReadOnlyList<SystemSettingDto>>(settings.Select(s => s.ToDto()).ToList());
    }

    public async Task<Result<SystemSettingDto>> UpsertSystemSettingAsync(
        Guid adminUserId,
        SystemSettingDto request,
        CancellationToken ct = default)
    {
        var repo = unitOfWork.GetRepository<SystemSetting, Guid>();
        var existing = await repo.GetByIdWithSpecAsync(new SystemSettingsSpecification(request.Key.Trim()), ct);

        if (existing is null)
        {
            existing = new SystemSetting
            {
                Key = request.Key.Trim(),
                Value = request.Value,
                Type = request.Type,
                Description = request.Description,
                CreatedAt = clock.UtcNow,
                CreatedBy = adminUserId.ToString()
            };
            await repo.AddAsync(existing, ct);
            await WriteAuditAsync(adminUserId, "CreateSetting", nameof(SystemSetting), existing.Key, null, request.Value, ct);
        }
        else
        {
            var oldValue = existing.Value;
            existing.Value = request.Value;
            existing.Type = request.Type;
            existing.Description = request.Description;
            existing.UpdatedAt = clock.UtcNow;
            existing.UpdatedBy = adminUserId.ToString();
            repo.Update(existing);
            await WriteAuditAsync(adminUserId, "UpdateSetting", nameof(SystemSetting), existing.Key, oldValue, request.Value, ct);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(existing.ToDto());
    }

    public async Task<Result<PaginationResponse<AuditLogDto>>> GetAuditLogsAsync(
        AuditLogQueryParams queryParams,
        CancellationToken ct = default)
    {
        var repo = unitOfWork.GetRepository<AuditLog, Guid>();
        var items = await repo.GetAllWithSpecAsync(new AuditLogsFilterSpecification(queryParams), ct);
        var count = await repo.GetCountAsync(new AuditLogsCountSpecification(queryParams), ct);

        return Result.Success(new PaginationResponse<AuditLogDto>(
            queryParams.PageIndex,
            queryParams.PageSize,
            count,
            items.Select(l => l.ToDto()).ToList()));
    }

    private static IReadOnlyList<string> InferRoles(AppUser user)
    {
        var roles = new List<string>();
        if (user.CustomerProfile is not null) roles.Add(RoleNames.Customer);
        if (user.CourierProfile is not null) roles.Add(RoleNames.Courier);
        if (user.StoreStaffAssignments.Count > 0) roles.Add(RoleNames.StoreStaff);
        return roles;
    }

    private async Task WriteAuditAsync(
        Guid adminUserId,
        string action,
        string entityType,
        string entityId,
        string? oldVal,
        string? newVal,
        CancellationToken ct)
    {
        await unitOfWork.GetRepository<AuditLog, Guid>().AddAsync(new AuditLog
        {
            UserId = adminUserId,
            Role = "Admin",
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValuesJson = oldVal,
            NewValuesJson = newVal,
            At = clock.UtcNow,
            CreatedAt = clock.UtcNow
        }, ct);
    }
}
