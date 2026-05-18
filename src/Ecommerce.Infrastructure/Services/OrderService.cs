using Ecommerce.Application.DTOs.Orders;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext _context;

    public OrderService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> CheckoutAsync(Guid userId, CreateOrderDto request)
    {
        var cart = await _context.Carts
            .Include(x => x.CartItems)
                .ThenInclude(x => x.ProductVariant)
                    .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (cart is null || !cart.CartItems.Any())
        {
            throw new Exception("Cart is empty");
        }

        foreach (var item in cart.CartItems)
        {
            if (item.ProductVariant.StockQuantity < item.Quantity)
            {
                throw new Exception($"Insufficient stock for variant {item.ProductVariant.SKU}");
            }
        }

        var order = new Order
        {
            UserId = userId,
            SubTotal = cart.TotalPrice,
            ShippingCost = request.ShippingCost,
            Tax = request.Tax,
            Discount = request.Discount,
            TotalPrice = cart.TotalPrice + request.ShippingCost + request.Tax - request.Discount,
            Status = OrderStatus.Pending,
            OrderItems = cart.CartItems.Select(item => new OrderItem
            {
                ProductVariantId = item.ProductVariantId,
                ProductNameSnapshot = item.ProductVariant.Product.Name,
                UnitPrice = item.UnitPriceSnapshot,
                Quantity = item.Quantity,
                TotalPrice = item.TotalPrice
            }).ToList()
        };

        foreach (var item in cart.CartItems)
        {
            item.ProductVariant.StockQuantity -= item.Quantity;
        }

        _context.Orders.Add(order);
        cart.CartItems.Clear();
        cart.TotalPrice = 0;

        await _context.SaveChangesAsync();

        return order.Id;
    }

    public async Task<List<OrderResponseDto>> GetAllForUserAsync(Guid userId)
    {
        var orders = await _context.Orders
            .Include(x => x.OrderItems)
                .ThenInclude(x => x.ProductVariant)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync();

        return orders.Select(x => new OrderResponseDto
        {
            Id = x.Id,
            SubTotal = x.SubTotal,
            ShippingCost = x.ShippingCost,
            Tax = x.Tax,
            Discount = x.Discount,
            TotalPrice = x.TotalPrice,
            Status = x.Status.ToString(),
            CreatedAtUtc = x.CreatedAtUtc,
            Items = x.OrderItems.Select(item => new OrderItemDto
            {
                ProductVariantId = item.ProductVariantId,
                ProductName = item.ProductNameSnapshot,
                SKU = item.ProductVariant.SKU,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice
            }).ToList()
        }).ToList();
    }

    public async Task<OrderResponseDto?> GetByIdAsync(Guid userId, Guid orderId)
    {
        var order = await _context.Orders
            .Include(x => x.OrderItems)
                .ThenInclude(x => x.ProductVariant)
            .FirstOrDefaultAsync(x => x.Id == orderId && x.UserId == userId);

        if (order is null)
        {
            return null;
        }

        return new OrderResponseDto
        {
            Id = order.Id,
            SubTotal = order.SubTotal,
            ShippingCost = order.ShippingCost,
            Tax = order.Tax,
            Discount = order.Discount,
            TotalPrice = order.TotalPrice,
            Status = order.Status.ToString(),
            CreatedAtUtc = order.CreatedAtUtc,
            Items = order.OrderItems.Select(item => new OrderItemDto
            {
                ProductVariantId = item.ProductVariantId,
                ProductName = item.ProductNameSnapshot,
                SKU = item.ProductVariant.SKU,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice
            }).ToList()
        };
    }

    public async Task CancelAsync(Guid userId, Guid orderId)
    {
        var order = await _context.Orders
            .Include(x => x.OrderItems)
                .ThenInclude(x => x.ProductVariant)
            .FirstOrDefaultAsync(x => x.Id == orderId && x.UserId == userId);

        if (order is null)
        {
            throw new Exception("Order not found");
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            return;
        }

        order.Status = OrderStatus.Cancelled;

        foreach (var item in order.OrderItems)
        {
            item.ProductVariant.StockQuantity += item.Quantity;
        }

        await _context.SaveChangesAsync();
    }
}
