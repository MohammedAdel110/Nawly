using Nawly.Domain.Models.Common;

namespace Nawly.Domain.Models.Ordering;

public class OrderItem : BaseGuidEntity
{
    public Guid OrderId { get; set; }
    public Guid? ProductId { get; set; } // Nullable FK kept for analytics; never alters snapshot
    public string ProductName { get; set; } = string.Empty;
    public string ProductNameAr { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
    public string? Note { get; set; }

    public ICollection<OrderItemOption> Options { get; set; } = [];
}
