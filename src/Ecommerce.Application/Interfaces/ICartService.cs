using Ecommerce.Application.DTOs.Carts;

namespace Ecommerce.Application.Interfaces;

public interface ICartService
{
    Task<CartResponseDto> GetCartAsync(Guid userId);

    Task AddToCartAsync(Guid userId, AddToCartDto request);

    Task UpdateQuantityAsync(
        Guid userId,
        Guid productVariantId,
        int quantity);

    Task RemoveItemAsync(
        Guid userId,
        Guid productVariantId);

    Task ClearCartAsync(Guid userId);
}