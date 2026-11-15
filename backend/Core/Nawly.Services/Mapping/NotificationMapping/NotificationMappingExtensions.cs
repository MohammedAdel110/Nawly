using Nawly.Domain.Models.Engagement;
using Nawly.Shared.DTOs.Notification;

namespace Nawly.Services.Mapping.NotificationMapping;

public static class NotificationMappingExtensions
{
    public static NotificationDto ToDto(this Notification notification) => new(
        notification.Id,
        notification.Type,
        notification.Title,
        notification.Body,
        notification.DataJson,
        notification.Channels,
        notification.IsRead,
        notification.ReadAt,
        notification.CreatedAt);
}
