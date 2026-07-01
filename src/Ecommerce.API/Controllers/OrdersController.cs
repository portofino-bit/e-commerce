using System.Security.Claims;
using Ecommerce.Application.DTOs.Orders;
using Ecommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers;

[Authorize]
[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        var userId = GetUserId();
        var orders = await _orderService.GetAllForUserAsync(userId);
        return Ok(orders);
    }

    [HttpGet("{orderId:guid}")]
    public async Task<IActionResult> GetOrder(Guid orderId)
    {
        var userId = GetUserId();
        var order = await _orderService.GetByIdAsync(userId, orderId);

        if (order is null)
        {
            return NotFound();
        }

        return Ok(order);
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout(CreateOrderDto request)
    {
        var userId = GetUserId();
        var orderId = await _orderService.CheckoutAsync(userId, request);
        return CreatedAtAction(nameof(GetOrder), new { orderId }, orderId);
    }

    [HttpPut("{orderId:guid}/cancel")]
    public async Task<IActionResult> CancelOrder(Guid orderId)
    {
        var userId = GetUserId();
        await _orderService.CancelAsync(userId, orderId);
        return NoContent();
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(userIdClaim!);
    }
}
