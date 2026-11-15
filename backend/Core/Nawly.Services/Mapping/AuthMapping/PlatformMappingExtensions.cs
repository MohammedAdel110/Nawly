using Nawly.Domain.Models.Platform;
using Nawly.Shared.DTOs.Admin;

namespace Nawly.Services.Mapping.AuthMapping;

public static class PlatformMappingExtensions
{
    public static SystemSettingDto ToDto(this SystemSetting setting) => new(
        setting.Key,
        setting.Value,
        setting.Type,
        setting.Description);

    public static AuditLogDto ToDto(this AuditLog log) => new(
        log.Id,
        log.UserId,
        log.Role,
        log.Action,
        log.EntityType,
        log.EntityId,
        log.OldValuesJson,
        log.NewValuesJson,
        log.IpAddress,
        log.CorrelationId,
        log.At);
}
