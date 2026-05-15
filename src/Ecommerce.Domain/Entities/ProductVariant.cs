using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Entities;

public class ProductVariant : BaseEntity
{
    public Guid ProductId { get; set; }

    public string SKU { get; set; } = default!;

    public string Color { get; set; } = default!;

    public string Size { get; set; } = default!;

    public decimal Price { get; set; }

    public int StockQuantity { get; set; }

    public Product Product { get; set; } = default!;
}