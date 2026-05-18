namespace Ecommerce.Application.DTOs.Carts;

public class AddToCartDto
{
    public Guid ProductVariantId { get; set; }

    public int Quantity { get; set; }
}