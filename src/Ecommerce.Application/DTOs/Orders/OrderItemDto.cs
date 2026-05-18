namespace Ecommerce.Application.DTOs.Orders;

public class OrderItemDto
{
    public Guid ProductVariantId { get; set; }

    public string ProductName { get; set; } = default!;

    public string SKU { get; set; } = default!;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalPrice { get; set; }
}
