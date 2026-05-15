using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Entities;

public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }

    public Guid ProductVariantId { get; set; }

    public string ProductNameSnapshot { get; set; } = default!;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal TotalPrice { get; set; }

    public Order Order { get; set; } = default!;

    public ProductVariant ProductVariant { get; set; } = default!;
}