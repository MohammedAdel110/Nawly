using Nawly.Abstraction.IServices.Auth;

namespace Nawly.Services.ServicesImplementation.Auth;

/// <summary>
/// System clock anchored to Africa/Cairo for store schedules and daily sequence boundaries.
/// </summary>
public sealed class SystemClock : IClock
{
    private static readonly TimeZoneInfo CairoTimeZone = ResolveCairoTimeZone();

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public DateTimeOffset CairoNow => ToCairoTime(UtcNow);

    public TimeOnly CairoLocalTime => TimeOnly.FromDateTime(CairoNow.DateTime);

    public DayOfWeek CairoDayOfWeek => CairoNow.DayOfWeek;

    public DateOnly CairoDate => DateOnly.FromDateTime(CairoNow.DateTime);

    public DateTimeOffset ToCairoTime(DateTimeOffset utcTime)
        => TimeZoneInfo.ConvertTime(utcTime, CairoTimeZone);

    private static TimeZoneInfo ResolveCairoTimeZone()
    {
        if (TimeZoneInfo.TryFindSystemTimeZoneById("Africa/Cairo", out var iana))
            return iana;

        if (TimeZoneInfo.TryFindSystemTimeZoneById("Egypt Standard Time", out var windows))
            return windows;

        return TimeZoneInfo.CreateCustomTimeZone(
            "Africa/Cairo",
            TimeSpan.FromHours(2),
            "Egypt Standard Time",
            "Egypt Standard Time");
    }
}
