namespace Nawly.Shared.EnumHelper.NotificationEnums;

[Flags]
public enum NotificationChannelFlags
{
    None = 0,
    InApp = 1,
    WebPush = 2,
    Sms = 4,
    Email = 8
}
