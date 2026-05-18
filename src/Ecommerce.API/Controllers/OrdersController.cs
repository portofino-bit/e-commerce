using Ecommerce.Application.DTOs.Orders;
using Ecommerce.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetOrders(Guid userId)
    {
        var orders = await _orderService.GetAllForUserAsync(userId);
        return Ok(orders);
    }

    [HttpGet("{userId:guid}/{orderId:guid}")]
    public async Task<IActionResult> GetOrder(Guid userId, Guid orderId)
    {
        var order = await _orderService.GetByIdAsync(userId, orderId);

        if (order is null)
        {
            return NotFound();
        }

        return Ok(order);
    }

    [HttpPost("{userId:guid}/checkout")]
    public async Task<IActionResult> Checkout(Guid userId, CreateOrderDto request)
    {
        var orderId = await _orderService.CheckoutAsync(userId, request);
        return CreatedAtAction(nameof(GetOrder), new { userId, orderId }, orderId);
    }

    [HttpPut("{userId:guid}/{orderId:guid}/cancel")]
    public async Task<IActionResult> CancelOrder(Guid userId, Guid orderId)
    {
        await _orderService.CancelAsync(userId, orderId);
        return NoContent();
    }
}
