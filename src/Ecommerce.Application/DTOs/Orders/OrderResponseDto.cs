namespace Ecommerce.Application.DTOs.Orders;

public class OrderResponseDto
{
    public Guid Id { get; set; }

    public decimal SubTotal { get; set; }

    public decimal ShippingCost { get; set; }

    public decimal Tax { get; set; }

    public decimal Discount { get; set; }

    public decimal TotalPrice { get; set; }

    public string Status { get; set; } = default!;

    public DateTime CreatedAtUtc { get; set; }

    public List<OrderItemDto> Items { get; set; } = [];
}
