using Nawly.Domain.Contracts.SpecificationPattern.BaseSpec;
using Nawly.Domain.Models.Identity;
using Nawly.Domain.Models.Platform;
using Nawly.Shared.Common.Params.Admin;

namespace Nawly.Services.Specifications.AdminSpec;

public sealed class UsersFilterSpecification : BaseSpecifications<AppUser, Guid>
{
    public UsersFilterSpecification(UserQueryParams queryParams)
    {
        var status = queryParams.Status;
        var search = queryParams.Search;
        var hasSearch = !string.IsNullOrWhiteSpace(search);

        Criteria = u => (status == null || u.Status == status)
            && (!hasSearch || u.FullName.Contains(search!) || (u.PhoneNumber != null && u.PhoneNumber.Contains(search!)) || (u.Email != null && u.Email.Contains(search!)));

        AddInclude(u => u.CustomerProfile!);
        AddInclude(u => u.CourierProfile!);
        AddOrderBy(u => u.CreatedAt, isDescending: true);
        ApplyPaging(queryParams.PageSize, queryParams.PageIndex);
        ApplyAsNoTracking();
    }
}

public sealed class UsersCountSpecification : BaseSpecifications<AppUser, Guid>
{
    public UsersCountSpecification(UserQueryParams queryParams)
    {
        var status = queryParams.Status;
        var search = queryParams.Search;
        var hasSearch = !string.IsNullOrWhiteSpace(search);

        Criteria = u => (status == null || u.Status == status)
            && (!hasSearch || u.FullName.Contains(search!) || (u.PhoneNumber != null && u.PhoneNumber.Contains(search!)) || (u.Email != null && u.Email.Contains(search!)));
    }
}

public sealed class AuditLogsFilterSpecification : BaseSpecifications<AuditLog, Guid>
{
    public AuditLogsFilterSpecification(AuditLogQueryParams queryParams)
    {
        var userId = queryParams.UserId;
        var entityType = queryParams.EntityType;
        var action = queryParams.Action;
        var fromDate = queryParams.FromDate;
        var toDate = queryParams.ToDate;
        var search = queryParams.Search;
        var hasSearch = !string.IsNullOrWhiteSpace(search);

        Criteria = l => !l.IsDeleted
            && (userId == null || l.UserId == userId)
            && (entityType == null || l.EntityType == entityType)
            && (action == null || l.Action == action)
            && (fromDate == null || l.At >= fromDate)
            && (toDate == null || l.At <= toDate)
            && (!hasSearch || l.Action.Contains(search!) || l.EntityType.Contains(search!) || l.EntityId.Contains(search!));

        AddOrderBy(l => l.At, isDescending: true);
        ApplyPaging(queryParams.PageSize, queryParams.PageIndex);
        ApplyAsNoTracking();
    }
}

public sealed class AuditLogsCountSpecification : BaseSpecifications<AuditLog, Guid>
{
    public AuditLogsCountSpecification(AuditLogQueryParams queryParams)
    {
        var userId = queryParams.UserId;
        var entityType = queryParams.EntityType;
        var action = queryParams.Action;
        var fromDate = queryParams.FromDate;
        var toDate = queryParams.ToDate;
        var search = queryParams.Search;
        var hasSearch = !string.IsNullOrWhiteSpace(search);

        Criteria = l => !l.IsDeleted
            && (userId == null || l.UserId == userId)
            && (entityType == null || l.EntityType == entityType)
            && (action == null || l.Action == action)
            && (fromDate == null || l.At >= fromDate)
            && (toDate == null || l.At <= toDate)
            && (!hasSearch || l.Action.Contains(search!) || l.EntityType.Contains(search!) || l.EntityId.Contains(search!));
    }
}

public sealed class SystemSettingsSpecification : BaseSpecifications<SystemSetting, Guid>
{
    public SystemSettingsSpecification(string? key = null)
        : base(s => !s.IsDeleted && (key == null || s.Key == key))
    {
        AddOrderBy(s => s.Key);
        if (key is null)
            ApplyAsNoTracking();
    }
}
