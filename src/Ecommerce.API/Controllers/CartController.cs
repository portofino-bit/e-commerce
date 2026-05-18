using Ecommerce.Application.DTOs.Carts;
using Ecommerce.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers;

[ApiController]
[Route("api/cart")]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetCart(Guid userId)
    {
        var cart = await _cartService.GetCartAsync(userId);
        return Ok(cart);
    }

    [HttpPost("{userId:guid}/items")]
    public async Task<IActionResult> AddToCart(Guid userId, AddToCartDto request)
    {
        await _cartService.AddToCartAsync(userId, request);
        return NoContent();
    }

    [HttpPut("{userId:guid}/items/{productVariantId:guid}")]
    public async Task<IActionResult> UpdateQuantity(
        Guid userId,
        Guid productVariantId,
        [FromBody] int quantity)
    {
        await _cartService.UpdateQuantityAsync(userId, productVariantId, quantity);
        return NoContent();
    }

    [HttpDelete("{userId:guid}/items/{productVariantId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid userId, Guid productVariantId)
    {
        await _cartService.RemoveItemAsync(userId, productVariantId);
        return NoContent();
    }

    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> ClearCart(Guid userId)
    {
        await _cartService.ClearCartAsync(userId);
        return NoContent();
    }
}
