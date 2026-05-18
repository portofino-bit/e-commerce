namespace Ecommerce.Application.DTOs.Orders;

public class CreateOrderDto
{
    public decimal ShippingCost { get; set; }

    public decimal Tax { get; set; }

    public decimal Discount { get; set; }
}
