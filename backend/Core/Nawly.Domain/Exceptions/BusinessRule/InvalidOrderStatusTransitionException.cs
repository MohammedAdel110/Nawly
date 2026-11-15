using Nawly.Shared.EnumHelper.OrderEnums;

namespace Nawly.Domain.Exceptions.BusinessRule;

/// <summary>
/// Thrown when an order attempts an illegal status transition (maps to HTTP 409 per SRS §5.1).
/// </summary>
public sealed class InvalidOrderStatusTransitionException : ConflictCustomException
{
    public OrderStatus From { get; }
    public OrderStatus To { get; }

    public InvalidOrderStatusTransitionException(OrderStatus from, OrderStatus to)
        : base($"Invalid order status transition from '{from}' to '{to}'.", "order.invalid_transition")
    {
        From = from;
        To = to;
    }
}
