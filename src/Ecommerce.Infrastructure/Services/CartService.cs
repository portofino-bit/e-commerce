using Ecommerce.Application.DTOs.Carts;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Services;

public class CartService : ICartService
{
    private readonly AppDbContext _context;

    public CartService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CartResponseDto> GetCartAsync(Guid userId)
    {
        var cart = await _context.Carts
            .Include(x => x.CartItems)
                .ThenInclude(x => x.ProductVariant)
                    .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (cart is null)
        {
            return new CartResponseDto();
        }

        return new CartResponseDto
        {
            CartId = cart.Id,
            TotalPrice = cart.TotalPrice,

            Items = cart.CartItems.Select(x =>
                new CartItemDto
                {
                    ProductVariantId = x.ProductVariantId,
                    ProductName = x.ProductVariant.Product.Name,
                    SKU = x.ProductVariant.SKU,
                    Quantity = x.Quantity,
                    UnitPrice = x.UnitPriceSnapshot,
                    TotalPrice = x.TotalPrice
                }).ToList()
        };
    }

    public async Task AddToCartAsync(
        Guid userId,
        AddToCartDto request)
    {
        var variant = await _context.ProductVariants
            .Include(x => x.Product)
            .FirstOrDefaultAsync(x =>
                x.Id == request.ProductVariantId);

        if (variant is null)
        {
            throw new Exception("Variant not found");
        }

        if (!variant.Product.IsPublished)
        {
            throw new Exception("Product is unpublished");
        }

        if (variant.StockQuantity < request.Quantity)
        {
            throw new Exception("Insufficient stock");
        }

        var cart = await _context.Carts
            .Include(x => x.CartItems)
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (cart is null)
        {
            cart = new Cart
            {
                UserId = userId
            };

            _context.Carts.Add(cart);
        }

        var existingItem = cart.CartItems
            .FirstOrDefault(x =>
                x.ProductVariantId == request.ProductVariantId);

        if (existingItem is not null)
        {
            existingItem.Quantity += request.Quantity;
        }
        else
        {
            cart.CartItems.Add(new CartItem
            {
                ProductVariantId = variant.Id,
                Quantity = request.Quantity,
                UnitPriceSnapshot = variant.Price
            });
        }

        cart.TotalPrice = cart.CartItems.Sum(x => x.TotalPrice);

        await _context.SaveChangesAsync();
    }

    public async Task UpdateQuantityAsync(
        Guid userId,
        Guid productVariantId,
        int quantity)
    {
        var cart = await _context.Carts
            .Include(x => x.CartItems)
                .ThenInclude(x => x.ProductVariant)
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (cart is null)
        {
            throw new Exception("Cart not found");
        }

        var item = cart.CartItems
            .FirstOrDefault(x =>
                x.ProductVariantId == productVariantId);

        if (item is null)
        {
            throw new Exception("Item not found");
        }

        if (item.ProductVariant.StockQuantity < quantity)
        {
            throw new Exception("Insufficient stock");
        }

        item.Quantity = quantity;

        cart.TotalPrice =
            cart.CartItems.Sum(x => x.TotalPrice);

        await _context.SaveChangesAsync();
    }

    public async Task RemoveItemAsync(
        Guid userId,
        Guid productVariantId)
    {
        var cart = await _context.Carts
            .Include(x => x.CartItems)
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (cart is null)
        {
            throw new Exception("Cart not found");
        }

        var item = cart.CartItems
            .FirstOrDefault(x =>
                x.ProductVariantId == productVariantId);

        if (item is null)
        {
            throw new Exception("Item not found");
        }

        cart.CartItems.Remove(item);

        cart.TotalPrice =
            cart.CartItems.Sum(x => x.TotalPrice);

        await _context.SaveChangesAsync();
    }

    public async Task ClearCartAsync(Guid userId)
    {
        var cart = await _context.Carts
            .Include(x => x.CartItems)
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (cart is null)
        {
            return;
        }

        cart.CartItems.Clear();

        cart.TotalPrice = 0;

        await _context.SaveChangesAsync();
    }
}