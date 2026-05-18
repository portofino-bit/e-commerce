namespace Ecommerce.Application.DTOs.Carts;

public class CartResponseDto
{
    public Guid CartId { get; set; }

    public decimal TotalPrice { get; set; }

    public List<CartItemDto> Items { get; set; } = [];
}