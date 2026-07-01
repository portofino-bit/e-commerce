using System.Security.Claims;
using Ecommerce.Application.DTOs.Carts;
using Ecommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers;

[Authorize]
[ApiController]
[Route("api/cart")]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var userId = GetUserId();
        var cart = await _cartService.GetCartAsync(userId);
        return Ok(cart);
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddToCart(AddToCartDto request)
    {
        var userId = GetUserId();
        await _cartService.AddToCartAsync(userId, request);
        return NoContent();
    }

    [HttpPut("items/{productVariantId:guid}")]
    public async Task<IActionResult> UpdateQuantity(
        Guid productVariantId,
        [FromBody] int quantity)
    {
        var userId = GetUserId();
        await _cartService.UpdateQuantityAsync(userId, productVariantId, quantity);
        return NoContent();
    }

    [HttpDelete("items/{productVariantId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid productVariantId)
    {
        var userId = GetUserId();
        await _cartService.RemoveItemAsync(userId, productVariantId);
        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> ClearCart()
    {
        var userId = GetUserId();
        await _cartService.ClearCartAsync(userId);
        return NoContent();
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(userIdClaim!);
    }
}
