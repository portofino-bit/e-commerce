using System.Net;
using System.Net.Http.Json;
using Ecommerce.Application.DTOs.Carts;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.IntegrationTests.Controllers;

public class CartControllerIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private AppDbContext _context = null!;

    public async Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<AppDbContext>(options =>
                    {
                        options.UseInMemoryDatabase(Guid.NewGuid().ToString());
                    });
                });
            });

        _client = _factory.CreateClient();
        
        var scope = _factory.Services.CreateAsyncScope();
        _context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        await _context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task GetCart_ShouldReturnEmptyCart_WhenUserHasNoCart()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/cart/{userId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsAsync<CartResponseDto>();
        Assert.NotNull(result);
        Assert.Equal(Guid.Empty, result.CartId);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetCart_ShouldReturnCartWithItems_WhenUserHasCart()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        var product = new Ecommerce.Domain.Entities.Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Product",
            Slug = "test-product",
            Description = "Test",
            CategoryId = Guid.NewGuid(),
            BrandId = Guid.NewGuid(),
            IsPublished = true
        };

        var variant = new Ecommerce.Domain.Entities.ProductVariant
        {
            Id = variantId,
            SKU = "TEST-001",
            Price = 100m,
            StockQuantity = 10,
            ProductId = product.Id,
            Product = product
        };

        var cart = new Ecommerce.Domain.Entities.Cart
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TotalPrice = 200m,
            CartItems = new List<Ecommerce.Domain.Entities.CartItem>
            {
                new Ecommerce.Domain.Entities.CartItem
                {
                    Id = Guid.NewGuid(),
                    ProductVariantId = variantId,
                    Quantity = 2,
                    UnitPriceSnapshot = 100m,
                    TotalPrice = 200m,
                    ProductVariant = variant
                }
            }
        };

        _context.Products.Add(product);
        _context.ProductVariants.Add(variant);
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/cart/{userId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsAsync<CartResponseDto>();
        Assert.NotNull(result);
        Assert.Equal(200m, result.TotalPrice);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task AddToCart_ShouldAddItem_WhenValidDataProvided()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        var product = new Ecommerce.Domain.Entities.Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Product",
            Slug = "test-product",
            Description = "Test",
            CategoryId = Guid.NewGuid(),
            BrandId = Guid.NewGuid(),
            IsPublished = true
        };

        var variant = new Ecommerce.Domain.Entities.ProductVariant
        {
            Id = variantId,
            SKU = "TEST-001",
            Price = 100m,
            StockQuantity = 10,
            ProductId = product.Id,
            Product = product
        };

        _context.Products.Add(product);
        _context.ProductVariants.Add(variant);
        await _context.SaveChangesAsync();

        var addToCartDto = new AddToCartDto
        {
            ProductVariantId = variantId,
            Quantity = 2
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/cart/{userId}/items", 
            addToCartDto);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task AddToCart_ShouldReturnBadRequest_WhenVariantDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        var addToCartDto = new AddToCartDto
        {
            ProductVariantId = variantId,
            Quantity = 2
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/cart/{userId}/items", 
            addToCartDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateQuantity_ShouldUpdateCartItem_WhenItemExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        var product = new Ecommerce.Domain.Entities.Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Product",
            Slug = "test-product",
            Description = "Test",
            CategoryId = Guid.NewGuid(),
            BrandId = Guid.NewGuid(),
            IsPublished = true
        };

        var variant = new Ecommerce.Domain.Entities.ProductVariant
        {
            Id = variantId,
            SKU = "TEST-001",
            Price = 100m,
            StockQuantity = 10,
            ProductId = product.Id,
            Product = product
        };

        var cart = new Ecommerce.Domain.Entities.Cart
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TotalPrice = 200m,
            CartItems = new List<Ecommerce.Domain.Entities.CartItem>
            {
                new Ecommerce.Domain.Entities.CartItem
                {
                    Id = Guid.NewGuid(),
                    ProductVariantId = variantId,
                    Quantity = 2,
                    UnitPriceSnapshot = 100m,
                    TotalPrice = 200m,
                    ProductVariant = variant
                }
            }
        };

        _context.Products.Add(product);
        _context.ProductVariants.Add(variant);
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/cart/{userId}/items/{variantId}",
            5);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task RemoveItem_ShouldRemoveCartItem_WhenItemExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        var product = new Ecommerce.Domain.Entities.Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Product",
            Slug = "test-product",
            Description = "Test",
            CategoryId = Guid.NewGuid(),
            BrandId = Guid.NewGuid(),
            IsPublished = true
        };

        var variant = new Ecommerce.Domain.Entities.ProductVariant
        {
            Id = variantId,
            SKU = "TEST-001",
            Price = 100m,
            StockQuantity = 10,
            ProductId = product.Id,
            Product = product
        };

        var cart = new Ecommerce.Domain.Entities.Cart
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TotalPrice = 200m,
            CartItems = new List<Ecommerce.Domain.Entities.CartItem>
            {
                new Ecommerce.Domain.Entities.CartItem
                {
                    Id = Guid.NewGuid(),
                    ProductVariantId = variantId,
                    Quantity = 2,
                    UnitPriceSnapshot = 100m,
                    TotalPrice = 200m,
                    ProductVariant = variant
                }
            }
        };

        _context.Products.Add(product);
        _context.ProductVariants.Add(variant);
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/cart/{userId}/items/{variantId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ClearCart_ShouldClearAllItems_WhenCartExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        var product = new Ecommerce.Domain.Entities.Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Product",
            Slug = "test-product",
            Description = "Test",
            CategoryId = Guid.NewGuid(),
            BrandId = Guid.NewGuid(),
            IsPublished = true
        };

        var variant = new Ecommerce.Domain.Entities.ProductVariant
        {
            Id = variantId,
            SKU = "TEST-001",
            Price = 100m,
            StockQuantity = 10,
            ProductId = product.Id,
            Product = product
        };

        var cart = new Ecommerce.Domain.Entities.Cart
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TotalPrice = 200m,
            CartItems = new List<Ecommerce.Domain.Entities.CartItem>
            {
                new Ecommerce.Domain.Entities.CartItem
                {
                    Id = Guid.NewGuid(),
                    ProductVariantId = variantId,
                    Quantity = 2,
                    UnitPriceSnapshot = 100m,
                    TotalPrice = 200m,
                    ProductVariant = variant
                }
            }
        };

        _context.Products.Add(product);
        _context.ProductVariants.Add(variant);
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/cart/{userId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
