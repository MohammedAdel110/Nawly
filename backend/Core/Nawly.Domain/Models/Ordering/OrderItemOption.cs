using Nawly.Domain.Models.Common;

namespace Nawly.Domain.Models.Ordering;

public class OrderItemOption : BaseGuidEntity
{
    public Guid OrderItemId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string OptionName { get; set; } = string.Empty;
    public decimal PriceDelta { get; set; }
}
