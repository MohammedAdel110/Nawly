using Nawly.Shared.EnumHelper.DeliveryEnums;

namespace Nawly.Domain.Exceptions.BusinessRule;

/// <summary>
/// Thrown when a delivery attempts an illegal sub-state transition (maps to HTTP 409 per SRS §5.1).
/// </summary>
public sealed class InvalidDeliveryStatusTransitionException : ConflictCustomException
{
    public DeliveryStatus From { get; }
    public DeliveryStatus To { get; }

    public InvalidDeliveryStatusTransitionException(DeliveryStatus from, DeliveryStatus to)
        : base($"Invalid delivery status transition from '{from}' to '{to}'.", "delivery.invalid_transition")
    {
        From = from;
        To = to;
    }
}
