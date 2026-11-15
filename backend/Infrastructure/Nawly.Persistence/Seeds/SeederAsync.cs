using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nawly.Domain.Models.Catalog;
using Nawly.Domain.Models.Geography;
using Nawly.Domain.Models.Identity;
using Nawly.Domain.Models.Platform;
using Nawly.Shared.EnumHelper.DeliveryEnums;
using Nawly.Shared.EnumHelper.StoreEnums;
using Nawly.Shared.EnumHelper.UserEnums;
using AppDbContext = Nawly.Persistence.Contexts.NawlyDbContext;

namespace Nawly.Persistence.Seeds;

/// <summary>
/// Idempotent seed pipeline calibrated for Nawa village operations (~2,000 DAU).
/// </summary>
public static class SeederAsync
{
    public static async Task SeedAsync(
        AppDbContext dbContext,
        RoleManager<AppRole> roleManager,
        UserManager<AppUser> userManager,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken ct = default)
    {
        var nowUtc = DateTimeOffset.UtcNow;

        await SeedRolesAsync(roleManager, logger);
        await SeedSystemSettingsAsync(dbContext, nowUtc, ct);
        await SeedSuperAdminAsync(userManager, configuration, nowUtc, logger);
        var (zoneCore, zoneOutskirts, areas) = await SeedNawaGeographyAsync(dbContext, nowUtc, ct);
        var categories = await SeedStoreCategoriesAsync(dbContext, nowUtc, ct);

        if (configuration.GetValue<bool>("Seeding:EnableDemoData"))
        {
            await SeedDemoDataAsync(
                dbContext,
                userManager,
                configuration,
                zoneCore,
                zoneOutskirts,
                areas,
                categories,
                nowUtc,
                logger,
                ct);
        }
    }

    private static async Task SeedRolesAsync(RoleManager<AppRole> roleManager, ILogger logger)
    {
        var descriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [RoleNames.Customer] = "Default customer role for ordering and tracking deliveries.",
            [RoleNames.StoreOwner] = "Store owner with full store profile, menu, and staff management.",
            [RoleNames.StoreStaff] = "Store staff operating live order queue and availability.",
            [RoleNames.Courier] = "Approved village delivery courier.",
            [RoleNames.AdminSuper] = "Platform super administrator with full governance access.",
            [RoleNames.AdminOperations] = "Operations administrator managing dispatch, stores, and couriers.",
            [RoleNames.AdminFinance] = "Finance administrator managing settlements, ledger, and refunds.",
            [RoleNames.AdminSupport] = "Support administrator handling tickets and order issues."
        };

        foreach (var roleName in RoleNames.All)
        {
            if (await roleManager.RoleExistsAsync(roleName))
                continue;

            var description = descriptions.GetValueOrDefault(roleName);
            var result = await roleManager.CreateAsync(new AppRole(roleName, description));
            if (!result.Succeeded)
            {
                logger.LogWarning(
                    "Failed to create role {Role}: {Errors}",
                    roleName,
                    string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    private static async Task SeedSystemSettingsAsync(AppDbContext dbContext, DateTimeOffset nowUtc, CancellationToken ct)
    {
        var defaults = new (string Key, string Value, string Type, string Description)[]
        {
            ("Finance:DefaultCommissionRate", "0.12", "decimal", "Default platform commission rate fraction (0.12 = 12%)."),
            ("Checkout:NewAccountCodLimitEgp", "300", "decimal", "Maximum COD order total in EGP for newly registered customer accounts."),
            ("Dispatch:OfferValiditySeconds", "30", "int", "Seconds a courier delivery offer remains open before expiring."),
            ("Dispatch:MaxWaves", "3", "int", "Maximum automatic dispatch waves before escalating to operations."),
            ("Order:StoreAcceptTimeoutMinutes", "5", "int", "Minutes a store has to accept a newly placed order before auto-cancel."),
            ("Courier:MaxCashOnHandLimitEgp", "2000", "decimal", "COD cash threshold requiring courier settlement before further COD runs.")
        };

        var existingKeys = await dbContext.SystemSettings
            .IgnoreQueryFilters()
            .Select(s => s.Key)
            .ToListAsync(ct);

        var existingSet = new HashSet<string>(existingKeys, StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value, type, description) in defaults)
        {
            if (existingSet.Contains(key))
                continue;

            await dbContext.SystemSettings.AddAsync(new SystemSetting
            {
                Key = key,
                Value = value,
                Type = type,
                Description = description,
                CreatedAt = nowUtc,
                CreatedBy = "system-seed"
            }, ct);
        }

        await dbContext.SaveChangesAsync(ct);
    }

    private static async Task SeedSuperAdminAsync(
        UserManager<AppUser> userManager,
        IConfiguration configuration,
        DateTimeOffset nowUtc,
        ILogger logger)
    {
        var adminPhone = configuration["Seeding:AdminPhone"] ?? "+201000000001";
        var existing = await userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == adminPhone);
        if (existing is not null)
            return;

        var adminPassword = configuration["Seeding:AdminPassword"] ?? "Admin@Nawly2026!";
        var admin = new AppUser
        {
            FullName = "Nawly Super Admin",
            PhoneNumber = adminPhone,
            PhoneNumberConfirmed = true,
            UserName = adminPhone,
            Email = "admin@nawly.local",
            EmailConfirmed = true,
            CreatedAt = nowUtc,
            CreatedBy = "system-seed"
        };

        var createResult = await userManager.CreateAsync(admin, adminPassword);
        if (!createResult.Succeeded)
        {
            logger.LogWarning(
                "Failed seeding SuperAdmin {Phone}: {Errors}",
                adminPhone,
                string.Join("; ", createResult.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(admin, RoleNames.AdminSuper);
    }

    private static async Task<(DeliveryZone CoreZone, DeliveryZone OutskirtsZone, List<Area> Areas)> SeedNawaGeographyAsync(
        AppDbContext dbContext,
        DateTimeOffset nowUtc,
        CancellationToken ct)
    {
        var existingZones = await dbContext.DeliveryZones.ToListAsync(ct);
        var existingAreas = await dbContext.Areas.ToListAsync(ct);

        if (existingZones.Count > 0 && existingAreas.Count > 0)
        {
            return (
                existingZones.First(),
                existingZones.Count > 1 ? existingZones[1] : existingZones.First(),
                existingAreas);
        }

        var coreZone = new DeliveryZone
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111110001"),
            Name = "Nawa Village Center",
            NameAr = "نوى - قلب القرية",
            BaseFee = 10m,
            PerKmFee = 2m,
            MinOrderAmount = 30m,
            FreeDeliveryThreshold = 250m,
            IsActive = true,
            CreatedAt = nowUtc,
            CreatedBy = "system-seed"
        };

        var outskirtsZone = new DeliveryZone
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111110002"),
            Name = "Nawa Outskirts & Hamlets",
            NameAr = "نوى - الأطراف والعزب",
            BaseFee = 15m,
            PerKmFee = 3m,
            MinOrderAmount = 50m,
            FreeDeliveryThreshold = 350m,
            IsActive = true,
            CreatedAt = nowUtc,
            CreatedBy = "system-seed"
        };

        if (existingZones.Count == 0)
            await dbContext.DeliveryZones.AddRangeAsync([coreZone, outskirtsZone], ct);
        else
        {
            coreZone = existingZones[0];
            outskirtsZone = existingZones.Count > 1 ? existingZones[1] : existingZones[0];
        }

        var seededAreas = new List<Area>
        {
            new() { Id = Guid.Parse("22222222-2222-2222-2222-222222220001"), ZoneId = coreZone.Id, Name = "El-Balad Center / Grand Mosque", NameAr = "وسط البلد / المسجد الكبير", SortOrder = 1, IsActive = true, CreatedAt = nowUtc, CreatedBy = "system-seed" },
            new() { Id = Guid.Parse("22222222-2222-2222-2222-222222220002"), ZoneId = coreZone.Id, Name = "Station Street", NameAr = "شارع المحطة", SortOrder = 2, IsActive = true, CreatedAt = nowUtc, CreatedBy = "system-seed" },
            new() { Id = Guid.Parse("22222222-2222-2222-2222-222222220003"), ZoneId = coreZone.Id, Name = "Northern Quarter", NameAr = "الناحية البحرية", SortOrder = 3, IsActive = true, CreatedAt = nowUtc, CreatedBy = "system-seed" },
            new() { Id = Guid.Parse("22222222-2222-2222-2222-222222220004"), ZoneId = coreZone.Id, Name = "Southern Quarter", NameAr = "الناحية القبلية", SortOrder = 4, IsActive = true, CreatedAt = nowUtc, CreatedBy = "system-seed" },
            new() { Id = Guid.Parse("22222222-2222-2222-2222-222222220005"), ZoneId = coreZone.Id, Name = "Schools & Health Unit", NameAr = "منطقة المدارس والوحدة الصحية", SortOrder = 5, IsActive = true, CreatedAt = nowUtc, CreatedBy = "system-seed" },
            new() { Id = Guid.Parse("22222222-2222-2222-2222-222222220006"), ZoneId = outskirtsZone.Id, Name = "Shibin El-Qanater Road", NameAr = "طريق شبين القناطر", SortOrder = 6, IsActive = true, CreatedAt = nowUtc, CreatedBy = "system-seed" },
            new() { Id = Guid.Parse("22222222-2222-2222-2222-222222220007"), ZoneId = outskirtsZone.Id, Name = "Qalyub Agricultural Road", NameAr = "طريق قليوب والزراعية", SortOrder = 7, IsActive = true, CreatedAt = nowUtc, CreatedBy = "system-seed" },
            new() { Id = Guid.Parse("22222222-2222-2222-2222-222222220008"), ZoneId = outskirtsZone.Id, Name = "Surrounding Hamlets & Ezbas", NameAr = "العزب والنجوع المحيطة بنوى", SortOrder = 8, IsActive = true, CreatedAt = nowUtc, CreatedBy = "system-seed" }
        };

        if (existingAreas.Count == 0)
        {
            await dbContext.Areas.AddRangeAsync(seededAreas, ct);
            await dbContext.SaveChangesAsync(ct);
            return (coreZone, outskirtsZone, seededAreas);
        }

        await dbContext.SaveChangesAsync(ct);
        return (coreZone, outskirtsZone, existingAreas);
    }

    private static async Task<List<StoreCategory>> SeedStoreCategoriesAsync(
        AppDbContext dbContext,
        DateTimeOffset nowUtc,
        CancellationToken ct)
    {
        var existing = await dbContext.StoreCategories.OrderBy(c => c.SortOrder).ToListAsync(ct);
        if (existing.Count > 0)
            return existing;

        var categories = new List<StoreCategory>
        {
            new() { Id = Guid.Parse("33333333-3333-3333-3333-333333330001"), Name = "Restaurants & Grills", NameAr = "مطاعم ومشويات", SortOrder = 1, IsActive = true, CreatedAt = nowUtc, CreatedBy = "system-seed" },
            new() { Id = Guid.Parse("33333333-3333-3333-3333-333333330002"), Name = "Grocery & Supermarket", NameAr = "سوبر ماركت وبقالة", SortOrder = 2, IsActive = true, CreatedAt = nowUtc, CreatedBy = "system-seed" },
            new() { Id = Guid.Parse("33333333-3333-3333-3333-333333330003"), Name = "Pharmacy & Wellness", NameAr = "صيدليات وعناية", SortOrder = 3, IsActive = true, CreatedAt = nowUtc, CreatedBy = "system-seed" },
            new() { Id = Guid.Parse("33333333-3333-3333-3333-333333330004"), Name = "Bakery & Sweets", NameAr = "مخابز وحلويات", SortOrder = 4, IsActive = true, CreatedAt = nowUtc, CreatedBy = "system-seed" },
            new() { Id = Guid.Parse("33333333-3333-3333-3333-333333330005"), Name = "Fresh Produce & Butcher", NameAr = "خضار وفاكهة وجزارة", SortOrder = 5, IsActive = true, CreatedAt = nowUtc, CreatedBy = "system-seed" }
        };

        await dbContext.StoreCategories.AddRangeAsync(categories, ct);
        await dbContext.SaveChangesAsync(ct);
        return categories;
    }

    private static async Task SeedDemoDataAsync(
        AppDbContext dbContext,
        UserManager<AppUser> userManager,
        IConfiguration configuration,
        DeliveryZone zoneCore,
        DeliveryZone zoneOutskirts,
        IReadOnlyList<Area> areas,
        IReadOnlyList<StoreCategory> categories,
        DateTimeOffset nowUtc,
        ILogger logger,
        CancellationToken ct)
    {
        if (await dbContext.Stores.AnyAsync(ct))
            return;

        var defaultArea = areas[0];
        var restaurantCategory = categories[0];
        var demoPassword = configuration["Seeding:DemoPassword"] ?? "NawlyDemo@2026!";

        // 1. Demo Store Owner & Store
        var owner = await EnsureDemoUserAsync(
            userManager,
            fullName: "حسن محمد - مطعم الأصيل",
            phone: "+201000000010",
            email: "owner@nawly.local",
            password: demoPassword,
            role: RoleNames.StoreOwner,
            nowUtc: nowUtc,
            logger: logger);

        if (owner is not null)
        {
            var storeId = Guid.Parse("44444444-4444-4444-4444-444444440001");
            var store = new Store
            {
                Id = storeId,
                Slug = "el-aseel-nawa",
                Name = "El-Aseel Restaurant & Koshary",
                NameAr = "مطعم وكشري الأصيل - نوى",
                Description = "Authentic Egyptian koshary, grills, and fresh daily trays in Nawa center.",
                DescriptionAr = "كشري مصري ومشويات وطواجن طازجة في قلب قرية نوى.",
                CategoryId = restaurantCategory.Id,
                AreaId = defaultArea.Id,
                Phone = "+201000000010",
                AddressText = "شارع المحطة، بجوار المسجد الكبير، نوى",
                Latitude = 30.2225,
                Longitude = 31.3055,
                Status = StoreStatus.Active,
                DeliveryMode = DeliveryMode.PlatformCourier,
                MinOrderAmount = 40m,
                CommissionRate = 0.12m,
                DefaultPrepMinutes = 20,
                IsAcceptingOrders = true,
                CreatedAt = nowUtc,
                CreatedBy = "system-seed"
            };
            store.RefreshNormalizedSearchText();

            store.DeliveryZones.Add(new StoreDeliveryZone { StoreId = store.Id, ZoneId = zoneCore.Id, CreatedAt = nowUtc });
            store.DeliveryZones.Add(new StoreDeliveryZone { StoreId = store.Id, ZoneId = zoneOutskirts.Id, CreatedAt = nowUtc });

            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                store.OpeningHours.Add(new StoreOpeningHour
                {
                    StoreId = store.Id,
                    DayOfWeek = day,
                    OpensAt = new TimeOnly(8, 0),
                    ClosesAt = new TimeOnly(23, 59),
                    CrossesMidnight = false,
                    CreatedAt = nowUtc
                });
            }

            store.Staff.Add(new StoreStaff
            {
                StoreId = store.Id,
                UserId = owner.Id,
                StoreRole = StoreRole.Owner,
                IsActive = true,
                CreatedAt = nowUtc
            });

            var catKoshary = new MenuCategory
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555550001"),
                StoreId = store.Id,
                Name = "Koshary & casseroles",
                NameAr = "الكشري والطواجن",
                SortOrder = 1,
                CreatedAt = nowUtc
            };

            var catGrills = new MenuCategory
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555550002"),
                StoreId = store.Id,
                Name = "Grills & Sandwiches",
                NameAr = "المشويات والسندوتشات",
                SortOrder = 2,
                CreatedAt = nowUtc
            };

            var kosharyClassic = new Product
            {
                Id = Guid.Parse("66666666-6666-6666-6666-666666660001"),
                StoreId = store.Id,
                MenuCategoryId = catKoshary.Id,
                Name = "El-Aseel Special Koshary",
                NameAr = "علبة كشري الأصيل المخصوص",
                Description = "Classic Egyptian koshary with crispy onions and spicy garlic sauce.",
                DescriptionAr = "كشري بالبصل المقرمش والصلصة والدقة.",
                Price = 35m,
                IsAvailable = true,
                PrepMinutes = 10,
                SortOrder = 1,
                CreatedAt = nowUtc
            };
            kosharyClassic.RefreshNormalizedSearchText();

            var sizeGroup = new ProductOptionGroup
            {
                Id = Guid.Parse("77777777-7777-7777-7777-777777770001"),
                ProductId = kosharyClassic.Id,
                Name = "Size",
                NameAr = "الحجم",
                IsRequired = true,
                MinSelect = 1,
                MaxSelect = 1,
                SortOrder = 1,
                CreatedAt = nowUtc
            };
            sizeGroup.Options.Add(new ProductOption { GroupId = sizeGroup.Id, Name = "Medium", NameAr = "وسط", PriceDelta = 0m, IsAvailable = true, SortOrder = 1, CreatedAt = nowUtc });
            sizeGroup.Options.Add(new ProductOption { GroupId = sizeGroup.Id, Name = "Large", NameAr = "كبير", PriceDelta = 15m, IsAvailable = true, SortOrder = 2, CreatedAt = nowUtc });
            sizeGroup.Options.Add(new ProductOption { GroupId = sizeGroup.Id, Name = "Family", NameAr = "عائلي", PriceDelta = 40m, IsAvailable = true, SortOrder = 3, CreatedAt = nowUtc });
            kosharyClassic.OptionGroups.Add(sizeGroup);

            var koftaMeal = new Product
            {
                Id = Guid.Parse("66666666-6666-6666-6666-666666660002"),
                StoreId = store.Id,
                MenuCategoryId = catGrills.Id,
                Name = "Charcoal Kofta Meal",
                NameAr = "وجبة كفتة مشوية على الفحم",
                Description = "Quarter kilo charcoal kofta with basmati rice, tahini, and baladi bread.",
                DescriptionAr = "ربع كيلو كفتة مشوية مع أرز بسمتي وطحينة وعيش بلدي.",
                Price = 145m,
                IsAvailable = true,
                PrepMinutes = 25,
                SortOrder = 1,
                CreatedAt = nowUtc
            };
            koftaMeal.RefreshNormalizedSearchText();

            await dbContext.Stores.AddAsync(store, ct);
            await dbContext.MenuCategories.AddRangeAsync([catKoshary, catGrills], ct);
            await dbContext.Products.AddRangeAsync([kosharyClassic, koftaMeal], ct);
        }

        // 2. Demo Customer + Default Nawa Address
        var customerUser = await EnsureDemoUserAsync(
            userManager,
            fullName: "أحمد علي",
            phone: "+201000000020",
            email: "customer@nawly.local",
            password: demoPassword,
            role: RoleNames.Customer,
            nowUtc: nowUtc,
            logger: logger);

        if (customerUser is not null && !await dbContext.CustomerProfiles.AnyAsync(c => c.UserId == customerUser.Id, ct))
        {
            var customerProfile = new CustomerProfile
            {
                Id = Guid.Parse("88888888-8888-8888-8888-888888880001"),
                UserId = customerUser.Id,
                CreatedAt = nowUtc
            };
            customerProfile.Addresses.Add(new Address
            {
                CustomerId = customerProfile.Id,
                Label = "المنزل",
                AreaId = defaultArea.Id,
                Description = "أمام مسجد النور، الدور الثاني",
                Building = "12",
                Floor = "2",
                Apartment = "4",
                ContactPhone = "+201000000020",
                Latitude = 30.2232,
                Longitude = 31.3061,
                IsDefault = true,
                CreatedAt = nowUtc
            });

            await dbContext.CustomerProfiles.AddAsync(customerProfile, ct);
        }

        // 3. Demo Courier (Approved & Ready in Nawa center)
        var courierUser = await EnsureDemoUserAsync(
            userManager,
            fullName: "محمود إبراهيم - مندوب نوى",
            phone: "+201000000030",
            email: "courier@nawly.local",
            password: demoPassword,
            role: RoleNames.Courier,
            nowUtc: nowUtc,
            logger: logger);

        if (courierUser is not null && !await dbContext.CourierProfiles.AnyAsync(c => c.UserId == courierUser.Id, ct))
        {
            var courierProfile = new CourierProfile
            {
                Id = Guid.Parse("99999999-9999-9999-9999-999999990001"),
                UserId = courierUser.Id,
                Status = CourierStatus.Approved,
                VehicleType = VehicleType.Motorcycle,
                NationalIdImageRef = "seed/courier-demo-nid.jpg",
                IsOnline = true,
                LastLat = 30.2229,
                LastLng = 31.3058,
                LastLocationAt = nowUtc,
                CreatedAt = nowUtc
            };

            await dbContext.CourierProfiles.AddAsync(courierProfile, ct);
        }

        await dbContext.SaveChangesAsync(ct);
    }

    private static async Task<AppUser?> EnsureDemoUserAsync(
        UserManager<AppUser> userManager,
        string fullName,
        string phone,
        string email,
        string password,
        string role,
        DateTimeOffset nowUtc,
        ILogger logger)
    {
        var existing = await userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone);
        if (existing is not null)
            return existing;

        var user = new AppUser
        {
            FullName = fullName,
            PhoneNumber = phone,
            PhoneNumberConfirmed = true,
            UserName = phone,
            Email = email,
            EmailConfirmed = true,
            CreatedAt = nowUtc,
            CreatedBy = "system-seed"
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            logger.LogWarning(
                "Failed seeding demo user {Phone}: {Errors}",
                phone,
                string.Join("; ", result.Errors.Select(e => e.Description)));
            return null;
        }

        await userManager.AddToRoleAsync(user, role);
        return user;
    }
}
