using Nawly.Abstraction.IServices.Auth;
using Nawly.Abstraction.IServices.Store;
using Nawly.Domain.Contracts.UnitOfWorkPattern;
using Nawly.Domain.Models.Catalog;
using Nawly.Domain.Models.Engagement;
using Nawly.Domain.Models.Geography;
using Nawly.Domain.Models.Identity;
using Nawly.Domain.ValueObjects;
using Nawly.Services.Mapping.StoreMapping;
using Nawly.Services.Specifications.OrderSpec;
using Nawly.Services.Specifications.StoreSpec;
using Nawly.Shared.Common.Pagination;
using Nawly.Shared.Common.Params.Store;
using Nawly.Shared.Common.Results;
using Nawly.Shared.DTOs.Store;
using Nawly.Shared.EnumHelper.OrderEnums;
using Nawly.Shared.EnumHelper.StoreEnums;
using OrderEntity = Nawly.Domain.Models.Ordering.Order;
using StoreEntity = Nawly.Domain.Models.Catalog.Store;

namespace Nawly.Services.ServicesImplementation.Store;

public sealed class StoreService(
    IUnitOfWork unitOfWork,
    IClock clock) : IStoreService
{
    public async Task<Result<PaginationResponse<StoreSummaryDto>>> SearchStoresAsync(
        StoreQueryParams queryParams,
        CancellationToken ct = default)
    {
        var repo = unitOfWork.GetRepository<StoreEntity, Guid>();
        var stores = await repo.GetAllWithSpecAsync(new StoresFilterSpecification(queryParams), ct);
        var total = await repo.GetCountAsync(new StoresCountSpecification(queryParams), ct);

        Guid? areaZoneId = null;
        if (queryParams.AreaId.HasValue)
        {
            var area = await unitOfWork.GetRepository<Area, Guid>().GetByIdAsync(queryParams.AreaId.Value, ct);
            areaZoneId = area?.ZoneId;
        }

        var localTime = clock.CairoLocalTime;
        var day = clock.CairoDayOfWeek;
        var utcNow = clock.UtcNow;

        var dtos = stores
            .Where(s => queryParams.OpenNow != true || s.IsCurrentlyOpen(localTime, day, utcNow))
            .Select(s => s.ToSummaryDto(localTime, day, utcNow, areaZoneId))
            .ToList();

        return Result.Success(new PaginationResponse<StoreSummaryDto>(queryParams.PageIndex, queryParams.PageSize, total, dtos));
    }

    public async Task<Result<StoreDetailsDto>> GetStoreBySlugAsync(string slug, CancellationToken ct = default)
    {
        var store = await unitOfWork
            .GetRepository<StoreEntity, Guid>()
            .GetByIdWithSpecAsync(new StoreWithDetailsSpecification(slug.Trim().ToLowerInvariant()), ct);

        if (store is null)
            return Result.Failure<StoreDetailsDto>(Error.NotFound("store.not_found", "Store not found."));

        return Result.Success(store.ToDetailsDto(clock.CairoLocalTime, clock.CairoDayOfWeek, clock.UtcNow));
    }

    public async Task<Result<StoreDetailsDto>> GetStoreByIdAsync(Guid storeId, CancellationToken ct = default)
    {
        var store = await unitOfWork
            .GetRepository<StoreEntity, Guid>()
            .GetByIdWithSpecAsync(new StoreWithDetailsSpecification(storeId), ct);

        if (store is null)
            return Result.Failure<StoreDetailsDto>(Error.NotFound("store.not_found", "Store not found."));

        return Result.Success(store.ToDetailsDto(clock.CairoLocalTime, clock.CairoDayOfWeek, clock.UtcNow));
    }

    public async Task<Result<IReadOnlyList<StoreCategoryDto>>> GetCategoriesAsync(
        bool activeOnly = true,
        CancellationToken ct = default)
    {
        var categories = await unitOfWork
            .GetRepository<StoreCategory, Guid>()
            .GetAllWithSpecAsync(new StoreCategoriesSpecification(activeOnly), ct);

        return Result.Success<IReadOnlyList<StoreCategoryDto>>(categories.Select(c => c.ToDto()).ToList());
    }

    public async Task<Result<StoreCategoryDto>> UpsertCategoryAsync(
        Guid? categoryId,
        UpsertStoreCategoryDto request,
        CancellationToken ct = default)
    {
        var repo = unitOfWork.GetRepository<StoreCategory, Guid>();

        if (!categoryId.HasValue)
        {
            var created = new StoreCategory
            {
                Name = request.Name.Trim(),
                NameAr = request.NameAr.Trim(),
                IconRef = request.IconRef,
                SortOrder = request.SortOrder,
                IsActive = request.IsActive,
                CreatedAt = clock.UtcNow
            };
            await repo.AddAsync(created, ct);
            await unitOfWork.SaveChangesAsync(ct);
            return Result.Success(created.ToDto());
        }

        var category = await repo.GetByIdAsync(categoryId.Value, ct);
        if (category is null || category.IsDeleted)
            return Result.Failure<StoreCategoryDto>(Error.NotFound("store.category_not_found", "Store category not found."));

        category.Name = request.Name.Trim();
        category.NameAr = request.NameAr.Trim();
        category.IconRef = request.IconRef;
        category.SortOrder = request.SortOrder;
        category.IsActive = request.IsActive;
        category.UpdatedAt = clock.UtcNow;

        repo.Update(category);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(category.ToDto());
    }

    public async Task<Result<StoreDetailsDto>> ApplyAsync(ApplyStoreDto request, CancellationToken ct = default)
    {
        if (!PhoneNumber.TryParseEgyptianMobile(request.OwnerPhone, out var ownerPhone) || ownerPhone is null)
            return Result.Failure<StoreDetailsDto>(Error.Validation("store.invalid_owner_phone", "Owner phone must be a valid Egyptian mobile number."));

        if (!PhoneNumber.TryParseEgyptianMobile(request.StorePhone, out var storePhone) || storePhone is null)
            return Result.Failure<StoreDetailsDto>(Error.Validation("store.invalid_store_phone", "Store phone must be a valid Egyptian mobile number."));

        var userRepo = unitOfWork.GetRepository<AppUser, Guid>();
        var existingUser = await userRepo.GetByIdWithSpecAsync(new UserByPhoneSpecification(ownerPhone.Value), ct);
        if (existingUser is not null)
            return Result.Failure<StoreDetailsDto>(Error.Conflict("store.owner_phone_exists", "An account already exists with this phone number."));

        var category = await unitOfWork.GetRepository<StoreCategory, Guid>().GetByIdAsync(request.CategoryId, ct);
        if (category is null || category.IsDeleted || !category.IsActive)
            return Result.Failure<StoreDetailsDto>(Error.NotFound("store.category_not_found", "Store category not found."));

        var area = await unitOfWork.GetRepository<Area, Guid>().GetByIdAsync(request.AreaId, ct);
        if (area is null || area.IsDeleted || !area.IsActive)
            return Result.Failure<StoreDetailsDto>(Error.NotFound("store.area_not_found", "Area not found."));

        var owner = new AppUser
        {
            FullName = request.OwnerFullName.Trim(),
            PhoneNumber = ownerPhone.Value,
            PhoneNumberConfirmed = true,
            UserName = ownerPhone.Value,
            NormalizedUserName = ownerPhone.Value.ToUpperInvariant(),
            SecurityStamp = Guid.NewGuid().ToString("N"),
            CreatedAt = clock.UtcNow
        };
        owner.PasswordHash = ServiceHelpers.HashPassword(owner, request.OwnerPassword);

        var store = new StoreEntity
        {
            Slug = ServiceHelpers.GenerateSlug(request.StoreName),
            Name = request.StoreName.Trim(),
            NameAr = request.StoreNameAr.Trim(),
            Description = request.Description?.Trim(),
            DescriptionAr = request.DescriptionAr?.Trim(),
            CategoryId = category.Id,
            Category = category,
            AreaId = area.Id,
            Area = area,
            AddressText = request.AddressText.Trim(),
            Phone = storePhone.Value,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Status = StoreStatus.Pending,
            DeliveryMode = request.DeliveryMode,
            MinOrderAmount = request.MinOrderAmount,
            DefaultPrepMinutes = request.DefaultPrepMinutes,
            CreatedAt = clock.UtcNow
        };
        store.RefreshNormalizedSearchText();

        foreach (var zoneId in request.DeliveryZoneIds.Distinct())
            store.DeliveryZones.Add(new StoreDeliveryZone { StoreId = store.Id, ZoneId = zoneId });

        store.Staff.Add(new StoreStaff
        {
            StoreId = store.Id,
            UserId = owner.Id,
            StoreRole = StoreRole.Owner,
            IsActive = true
        });

        await userRepo.AddAsync(owner, ct);
        await unitOfWork.GetRepository<StoreEntity, Guid>().AddAsync(store, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(store.ToDetailsDto(clock.CairoLocalTime, clock.CairoDayOfWeek, clock.UtcNow));
    }

    public async Task<Result<StoreDetailsDto>> UpdateSettingsAsync(
        Guid storeId,
        Guid actorUserId,
        UpdateStoreSettingsDto request,
        CancellationToken ct = default)
    {
        if (!PhoneNumber.TryParseEgyptianMobile(request.Phone, out var normalizedPhone) || normalizedPhone is null)
            return Result.Failure<StoreDetailsDto>(Error.Validation("store.invalid_phone", "Store phone must be a valid Egyptian mobile number."));

        var repo = unitOfWork.GetRepository<StoreEntity, Guid>();
        var store = await repo.GetByIdWithSpecAsync(new StoreWithDetailsSpecification(storeId, asNoTracking: false), ct);
        if (store is null)
            return Result.Failure<StoreDetailsDto>(Error.NotFound("store.not_found", "Store not found."));

        store.Name = request.Name.Trim();
        store.NameAr = request.NameAr.Trim();
        store.Description = request.Description?.Trim();
        store.DescriptionAr = request.DescriptionAr?.Trim();
        store.Phone = normalizedPhone.Value;
        store.AddressText = request.AddressText.Trim();
        store.Latitude = request.Latitude;
        store.Longitude = request.Longitude;
        store.DeliveryMode = request.DeliveryMode;
        store.MinOrderAmount = request.MinOrderAmount;
        store.DefaultPrepMinutes = request.DefaultPrepMinutes;
        store.BusyExtraMinutes = request.BusyExtraMinutes;
        store.IsAcceptingOrders = request.IsAcceptingOrders;
        store.UpdatedBy = actorUserId.ToString();
        store.UpdatedAt = clock.UtcNow;
        store.RefreshNormalizedSearchText();

        repo.Update(store);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(store.ToDetailsDto(clock.CairoLocalTime, clock.CairoDayOfWeek, clock.UtcNow));
    }

    public async Task<Result> UpdateOpeningHoursAsync(
        Guid storeId,
        Guid actorUserId,
        IReadOnlyList<StoreOpeningHourDto> hours,
        CancellationToken ct = default)
    {
        var repo = unitOfWork.GetRepository<StoreEntity, Guid>();
        var store = await repo.GetByIdWithSpecAsync(new StoreWithDetailsSpecification(storeId, asNoTracking: false), ct);
        if (store is null)
            return Result.Failure(Error.NotFound("store.not_found", "Store not found."));

        var hourRepo = unitOfWork.GetRepository<StoreOpeningHour, Guid>();
        foreach (var existing in store.OpeningHours.Where(h => !h.IsDeleted))
        {
            existing.IsDeleted = true;
            existing.DeletedAt = clock.UtcNow;
            hourRepo.Update(existing);
        }

        foreach (var h in hours)
        {
            await hourRepo.AddAsync(new StoreOpeningHour
            {
                StoreId = store.Id,
                DayOfWeek = h.DayOfWeek,
                OpensAt = h.OpensAt,
                ClosesAt = h.ClosesAt,
                CrossesMidnight = h.CrossesMidnight,
                CreatedAt = clock.UtcNow
            }, ct);
        }

        store.UpdatedBy = actorUserId.ToString();
        store.UpdatedAt = clock.UtcNow;
        repo.Update(store);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> PauseStoreAsync(
        Guid storeId,
        Guid actorUserId,
        PauseStoreDto request,
        CancellationToken ct = default)
    {
        var repo = unitOfWork.GetRepository<StoreEntity, Guid>();
        var store = await repo.GetByIdAsync(storeId, ct);
        if (store is null || store.IsDeleted)
            return Result.Failure(Error.NotFound("store.not_found", "Store not found."));

        if (request.UntilTomorrow)
        {
            var tomorrow = clock.CairoDate.AddDays(1);
            var cairoTomorrowMorning = new DateTimeOffset(tomorrow.Year, tomorrow.Month, tomorrow.Day, 6, 0, 0, clock.CairoNow.Offset);
            store.PausedUntil = cairoTomorrowMorning.ToUniversalTime();
        }
        else
        {
            var minutes = Math.Clamp(request.DurationMinutes ?? 30, 5, 720);
            store.PausedUntil = clock.UtcNow.AddMinutes(minutes);
        }

        store.UpdatedBy = actorUserId.ToString();
        store.UpdatedAt = clock.UtcNow;
        repo.Update(store);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> ResumeStoreAsync(Guid storeId, Guid actorUserId, CancellationToken ct = default)
    {
        var repo = unitOfWork.GetRepository<StoreEntity, Guid>();
        var store = await repo.GetByIdAsync(storeId, ct);
        if (store is null || store.IsDeleted)
            return Result.Failure(Error.NotFound("store.not_found", "Store not found."));

        store.PausedUntil = null;
        store.IsAcceptingOrders = true;
        store.UpdatedBy = actorUserId.ToString();
        store.UpdatedAt = clock.UtcNow;
        repo.Update(store);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<StoreStaffDto>>> GetStaffAsync(Guid storeId, CancellationToken ct = default)
    {
        var staff = await unitOfWork
            .GetRepository<StoreStaff, Guid>()
            .GetAllWithSpecAsync(new StoreStaffSpecification(storeId), ct);

        return Result.Success<IReadOnlyList<StoreStaffDto>>(staff.Select(s => s.ToDto()).ToList());
    }

    public async Task<Result<StoreStaffDto>> AddStaffAsync(
        Guid storeId,
        Guid actorUserId,
        AddStoreStaffDto request,
        CancellationToken ct = default)
    {
        if (!PhoneNumber.TryParseEgyptianMobile(request.PhoneNumber, out var phone) || phone is null)
            return Result.Failure<StoreStaffDto>(Error.Validation("store.invalid_phone", "Staff phone must be a valid Egyptian mobile number."));

        var store = await unitOfWork.GetRepository<StoreEntity, Guid>().GetByIdAsync(storeId, ct);
        if (store is null || store.IsDeleted)
            return Result.Failure<StoreStaffDto>(Error.NotFound("store.not_found", "Store not found."));

        var userRepo = unitOfWork.GetRepository<AppUser, Guid>();
        var user = await userRepo.GetByIdWithSpecAsync(new UserByPhoneSpecification(phone.Value), ct);

        if (user is null)
        {
            user = new AppUser
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
            await userRepo.AddAsync(user, ct);
        }

        var staffRepo = unitOfWork.GetRepository<StoreStaff, Guid>();
        var existingStaff = await staffRepo.GetByIdWithSpecAsync(new StoreStaffByUserSpecification(storeId, user.Id), ct);
        if (existingStaff is not null)
            return Result.Failure<StoreStaffDto>(Error.Conflict("store.staff_already_exists", "User is already assigned to this store."));

        var assignment = new StoreStaff
        {
            StoreId = storeId,
            UserId = user.Id,
            User = user,
            StoreRole = request.StoreRole,
            IsActive = true,
            CreatedAt = clock.UtcNow,
            CreatedBy = actorUserId.ToString()
        };

        await staffRepo.AddAsync(assignment, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(assignment.ToDto());
    }

    public async Task<Result> RemoveStaffAsync(
        Guid storeId,
        Guid actorUserId,
        Guid staffUserId,
        CancellationToken ct = default)
    {
        var staffRepo = unitOfWork.GetRepository<StoreStaff, Guid>();
        var assignment = await staffRepo.GetByIdWithSpecAsync(new StoreStaffByUserSpecification(storeId, staffUserId), ct);
        if (assignment is null)
            return Result.Failure(Error.NotFound("store.staff_not_found", "Store staff assignment not found."));

        if (assignment.StoreRole == StoreRole.Owner)
            return Result.Failure(Error.Conflict("store.cannot_remove_owner", "Store owner cannot be removed."));

        assignment.IsActive = false;
        assignment.IsDeleted = true;
        assignment.DeletedAt = clock.UtcNow;
        assignment.UpdatedBy = actorUserId.ToString();

        staffRepo.Update(assignment);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<StoreDashboardDto>> GetDashboardAsync(Guid storeId, CancellationToken ct = default)
    {
        var store = await unitOfWork.GetRepository<StoreEntity, Guid>().GetByIdAsync(storeId, ct);
        if (store is null || store.IsDeleted)
            return Result.Failure<StoreDashboardDto>(Error.NotFound("store.not_found", "Store not found."));

        var cairoDate = clock.CairoDate;
        var todayStartUtc = new DateTimeOffset(cairoDate.Year, cairoDate.Month, cairoDate.Day, 0, 0, 0, clock.CairoNow.Offset).ToUniversalTime();

        var orderRepo = unitOfWork.GetRepository<OrderEntity, Guid>();
        var todayOrders = await orderRepo.GetAllWithSpecAsync(new CustomerOrdersCountSinceSpecification(storeId, todayStartUtc), ct);
        var activeOrders = await orderRepo.GetAllWithSpecAsync(new PendingStoreOrdersSpecification(storeId), ct);

        var todayRevenue = todayOrders
            .Where(o => o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Rejected)
            .Sum(o => o.Total);

        var pendingCount = activeOrders.Count(o => o.Status == OrderStatus.Placed);

        return Result.Success(new StoreDashboardDto(
            todayOrders.Count,
            decimal.Round(todayRevenue, 2, MidpointRounding.AwayFromZero),
            pendingCount,
            activeOrders.Count,
            store.DefaultPrepMinutes,
            store.IsAcceptingOrders,
            store.PausedUntil));
    }

    public async Task<Result<IReadOnlyList<StoreSummaryDto>>> GetFavoritesAsync(Guid userId, CancellationToken ct = default)
    {
        var customer = await ServiceHelpers.ResolveCustomerProfileAsync(unitOfWork, userId, ct);
        if (customer is null)
            return Result.Success<IReadOnlyList<StoreSummaryDto>>([]);

        var favorites = await unitOfWork
            .GetRepository<Favorite, Guid>()
            .GetAllWithSpecAsync(new CustomerFavoritesSpecification(customer.Id), ct);

        var dtos = favorites
            .Select(f => f.Store.ToSummaryDto(clock.CairoLocalTime, clock.CairoDayOfWeek, clock.UtcNow))
            .ToList();

        return Result.Success<IReadOnlyList<StoreSummaryDto>>(dtos);
    }

    public async Task<Result> AddFavoriteAsync(Guid userId, Guid storeId, CancellationToken ct = default)
    {
        var store = await unitOfWork.GetRepository<StoreEntity, Guid>().GetByIdAsync(storeId, ct);
        if (store is null || store.IsDeleted || store.Status != StoreStatus.Active)
            return Result.Failure(Error.NotFound("store.not_found", "Store not found."));

        var customer = await ServiceHelpers.ResolveOrCreateCustomerProfileAsync(unitOfWork, userId, ct);
        var repo = unitOfWork.GetRepository<Favorite, Guid>();

        var existing = await repo.GetByIdWithSpecAsync(new CustomerFavoriteByPairSpecification(customer.Id, storeId), ct);
        if (existing is not null)
            return Result.Success();

        await repo.AddAsync(new Favorite
        {
            CustomerId = customer.Id,
            StoreId = storeId,
            CreatedAt = clock.UtcNow
        }, ct);

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> RemoveFavoriteAsync(Guid userId, Guid storeId, CancellationToken ct = default)
    {
        var customer = await ServiceHelpers.ResolveCustomerProfileAsync(unitOfWork, userId, ct);
        if (customer is null)
            return Result.Success();

        var repo = unitOfWork.GetRepository<Favorite, Guid>();
        var existing = await repo.GetByIdWithSpecAsync(new CustomerFavoriteByPairSpecification(customer.Id, storeId), ct);
        if (existing is null)
            return Result.Success();

        existing.IsDeleted = true;
        existing.DeletedAt = clock.UtcNow;
        repo.Update(existing);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
