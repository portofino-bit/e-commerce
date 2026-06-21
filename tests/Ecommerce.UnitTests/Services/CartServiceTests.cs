using Ecommerce.Application.DTOs.Carts;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Infrastructure.Services;
using Ecommerce.UnitTests.Builders;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.UnitTests.Services;

public class CartServiceTests
{
    private AppDbContext _context = null!;
    private ICartService _cartService = null!;

    private AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetCartAsync_ShouldReturnEmptyCart_WhenUserHasNoCart()
    {
        // Arrange
        _context = CreateInMemoryContext();
        await _context.Database.EnsureCreatedAsync();
        
        var userId = Guid.NewGuid();
        _cartService = new CartService(_context);

        // Act
        var result = await _cartService.GetCartAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(Guid.Empty, result.CartId);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetCartAsync_ShouldReturnCartWithItems_WhenUserHasCart()
    {
        // Arrange
        _context = CreateInMemoryContext();
        
        var userId = Guid.NewGuid();
        var builder = new TestDataBuilder()
            .WithUserId(userId)
            .WithPrice(75m)
            .WithQuantity(2);
        var (product, variant, cart, _) = builder.BuildAllForCheckout();

        _context.Products.Add(product);
        _context.ProductVariants.Add(variant);
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();
        
        _cartService = new CartService(_context);

        // Act
        var result = await _cartService.GetCartAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(150m, result.TotalPrice);
        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].Quantity);
    }

    [Fact]
    public async Task AddToCartAsync_ShouldAddNewItem_WhenProductVariantExists()
    {
        // Arrange
        _context = CreateInMemoryContext();
        
        var userId = Guid.NewGuid();
        var builder = new TestDataBuilder()
            .WithUserId(userId)
            .WithPrice(99.99m);
        var (product, variant, _, _) = builder.BuildAllForCheckout();
        var variantId = variant.Id;

        var addToCartDto = new AddToCartDto
        {
            ProductVariantId = variantId,
            Quantity = 2
        };

        _context.Products.Add(product);
        _context.ProductVariants.Add(variant);
        await _context.SaveChangesAsync();
        
        _cartService = new CartService(_context);

        // Act
        await _cartService.AddToCartAsync(userId, addToCartDto);

        // Assert
        var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
        Assert.NotNull(cart);
        Assert.Single(cart.CartItems);
    }

    [Fact]
    public async Task AddToCartAsync_ShouldThrowException_WhenVariantNotFound()
    {
        // Arrange
        _context = CreateInMemoryContext();
        await _context.Database.EnsureCreatedAsync();
        
        var userId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        var addToCartDto = new AddToCartDto
        {
            ProductVariantId = variantId,
            Quantity = 2
        };

        _cartService = new CartService(_context);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(
            async () => await _cartService.AddToCartAsync(userId, addToCartDto));
    }

    [Fact]
    public async Task AddToCartAsync_ShouldThrowException_WhenProductUnpublished()
    {
        // Arrange
        _context = CreateInMemoryContext();
        
        var userId = Guid.NewGuid();
        var builder = new TestDataBuilder()
            .WithUserId(userId)
            .WithPrice(99.99m)
            .WithIsPublished(false);
        var (product, variant, _, _) = builder.BuildAllForCheckout();
        var variantId = variant.Id;

        var addToCartDto = new AddToCartDto
        {
            ProductVariantId = variantId,
            Quantity = 1
        };

        _context.Products.Add(product);
        _context.ProductVariants.Add(variant);
        await _context.SaveChangesAsync();
        
        _cartService = new CartService(_context);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(
            async () => await _cartService.AddToCartAsync(userId, addToCartDto));
    }

    [Fact]
    public async Task AddToCartAsync_ShouldThrowException_WhenInsufficientStock()
    {
        // Arrange
        _context = CreateInMemoryContext();
        
        var userId = Guid.NewGuid();
        var builder = new TestDataBuilder()
            .WithUserId(userId)
            .WithPrice(99.99m)
            .WithStockQuantity(1)
            .WithQuantity(5);
        var (product, variant, _, _) = builder.BuildAllForCheckout();
        var variantId = variant.Id;

        var addToCartDto = new AddToCartDto
        {
            ProductVariantId = variantId,
            Quantity = 5 // Requesting more than stock
        };

        _context.Products.Add(product);
        _context.ProductVariants.Add(variant);
        await _context.SaveChangesAsync();
        
        _cartService = new CartService(_context);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(
            async () => await _cartService.AddToCartAsync(userId, addToCartDto));
    }

    [Fact]
    public async Task UpdateQuantityAsync_ShouldUpdateCartItemQuantity_WhenItemExists()
    {
        // Arrange
        _context = CreateInMemoryContext();
        
        var userId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var builder = new TestDataBuilder()
            .WithUserId(userId)
            .WithVariantId(variantId);
        var (product, variant, cart, _) = builder.BuildAllForCheckout();

        _context.Products.Add(product);
        _context.ProductVariants.Add(variant);
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();
        
        _cartService = new CartService(_context);

        // Act
        await _cartService.UpdateQuantityAsync(userId, variantId, 5);

        // Assert
        var updatedCart = await _context.Carts.Include(c => c.CartItems).FirstOrDefaultAsync(c => c.UserId == userId);
        Assert.NotNull(updatedCart);
        Assert.Equal(5, updatedCart.CartItems.First().Quantity);
    }

    [Fact]
    public async Task RemoveItemAsync_ShouldRemoveCartItem_WhenItemExists()
    {
        // Arrange
        _context = CreateInMemoryContext();
        
        var userId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var builder = new TestDataBuilder()
            .WithUserId(userId)
            .WithVariantId(variantId);
        var (product, variant, cart, _) = builder.BuildAllForCheckout();

        _context.Products.Add(product);
        _context.ProductVariants.Add(variant);
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();
        
        _cartService = new CartService(_context);

        // Act
        await _cartService.RemoveItemAsync(userId, variantId);

        // Assert
        var updatedCart = await _context.Carts.Include(c => c.CartItems).FirstOrDefaultAsync(c => c.UserId == userId);
        Assert.NotNull(updatedCart);
        Assert.Empty(updatedCart.CartItems);
    }

    [Fact]
    public async Task ClearCartAsync_ShouldRemoveAllItems_WhenCartExists()
    {
        // Arrange
        _context = CreateInMemoryContext();
        
        var userId = Guid.NewGuid();
        var builder1 = new TestDataBuilder()
            .WithUserId(userId)
            .WithSku("SKU-001");
        var (product1, variant1, _, _) = builder1.BuildAllForCheckout();

        var builder2 = new TestDataBuilder()
            .WithUserId(userId)
            .WithSku("SKU-002");
        var (product2, variant2, _, _) = builder2.BuildAllForCheckout();

        var cart = new TestDataBuilder()
            .WithUserId(userId)
            .BuildCartWithMultipleItems(new List<(ProductVariant, int, decimal)>
            {
                (variant1, 2, 100m),
                (variant2, 3, 100m)
            });

        _context.Products.AddRange(product1, product2);
        _context.ProductVariants.AddRange(variant1, variant2);
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();
        
        _cartService = new CartService(_context);

        // Act
        await _cartService.ClearCartAsync(userId);

        // Assert
        var clearedCart = await _context.Carts.Include(c => c.CartItems).FirstOrDefaultAsync(c => c.UserId == userId);
        Assert.NotNull(clearedCart);
        Assert.Empty(clearedCart.CartItems);
    }
}