using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Entities;

public class CartItem : BaseEntity
{
    public Guid CartId { get; set; }

    public Guid ProductVariantId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPriceSnapshot { get; set; }

    public Cart Cart { get; set; } = default!;

    public ProductVariant ProductVariant { get; set; } = default!;
}