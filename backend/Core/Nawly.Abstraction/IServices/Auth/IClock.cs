namespace Nawly.Abstraction.IServices.Auth;

/// <summary>
/// Provides testable access to UTC time and Africa/Cairo business hours time.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
    DateTimeOffset CairoNow { get; }
    TimeOnly CairoLocalTime { get; }
    DayOfWeek CairoDayOfWeek { get; }
    DateOnly CairoDate { get; }
    DateTimeOffset ToCairoTime(DateTimeOffset utcTime);
}
