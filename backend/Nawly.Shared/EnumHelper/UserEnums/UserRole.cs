namespace Nawly.Shared.EnumHelper.UserEnums;

public enum UserRole
{
    Customer = 0,
    StoreOwner = 1,
    StoreStaff = 2,
    Courier = 3,
    AdminSuper = 4,
    AdminOperations = 5,
    AdminFinance = 6,
    AdminSupport = 7
}

/// <summary>
/// Canonical ASP.NET Identity role names matching SRS §6.3.
/// </summary>
public static class RoleNames
{
    public const string Customer = nameof(UserRole.Customer);
    public const string StoreOwner = nameof(UserRole.StoreOwner);
    public const string StoreStaff = nameof(UserRole.StoreStaff);
    public const string Courier = nameof(UserRole.Courier);
    public const string AdminSuper = nameof(UserRole.AdminSuper);
    public const string AdminOperations = nameof(UserRole.AdminOperations);
    public const string AdminFinance = nameof(UserRole.AdminFinance);
    public const string AdminSupport = nameof(UserRole.AdminSupport);

    public static readonly IReadOnlyList<string> All =
    [
        Customer,
        StoreOwner,
        StoreStaff,
        Courier,
        AdminSuper,
        AdminOperations,
        AdminFinance,
        AdminSupport
    ];
}
