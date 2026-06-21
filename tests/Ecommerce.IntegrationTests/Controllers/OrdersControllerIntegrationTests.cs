using System.Net;
using System.Net.Http.Json;
using Ecommerce.Application.DTOs.Orders;
using Ecommerce.Domain.Enums;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.IntegrationTests.Controllers;

public class OrdersControllerIntegrationTests : IAsyncLifetime
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
    public async Task GetOrders_ShouldReturnAllUserOrders()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var order1 = new Ecommerce.Domain.Entities.Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SubTotal = 100m,
            ShippingCost = 10m,
            Tax = 5m,
            Discount = 0m,
            TotalPrice = 115m,
            Status = OrderStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
            OrderItems = new List<Ecommerce.Domain.Entities.OrderItem>()
        };

        var order2 = new Ecommerce.Domain.Entities.Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SubTotal = 200m,
            ShippingCost = 10m,
            Tax = 10m,
            Discount = 0m,
            TotalPrice = 220m,
            Status = OrderStatus.Completed,
            CreatedAtUtc = DateTime.UtcNow,
            OrderItems = new List<Ecommerce.Domain.Entities.OrderItem>()
        };

        _context.Orders.AddRange(order1, order2);
        await _context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/orders/{userId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsAsync<List<OrderResponseDto>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetOrders_ShouldReturnEmptyList_WhenUserHasNoOrders()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/orders/{userId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsAsync<List<OrderResponseDto>>();
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetOrder_ShouldReturnOrder_WhenOrderExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var order = new Ecommerce.Domain.Entities.Order
        {
            Id = orderId,
            UserId = userId,
            SubTotal = 100m,
            ShippingCost = 10m,
            Tax = 5m,
            Discount = 0m,
            TotalPrice = 115m,
            Status = OrderStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow,
            OrderItems = new List<Ecommerce.Domain.Entities.OrderItem>()
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/orders/{userId}/{orderId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsAsync<OrderResponseDto>();
        Assert.NotNull(result);
        Assert.Equal(115m, result.TotalPrice);
    }

    [Fact]
    public async Task GetOrder_ShouldReturnNotFound_WhenOrderDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/orders/{userId}/{orderId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Checkout_ShouldCreateOrder_WhenValidCartExists()
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
            TotalPrice = 100m,
            CartItems = new List<Ecommerce.Domain.Entities.CartItem>
            {
                new Ecommerce.Domain.Entities.CartItem
                {
                    Id = Guid.NewGuid(),
                    ProductVariantId = variantId,
                    Quantity = 1,
                    UnitPriceSnapshot = 100m,
                    TotalPrice = 100m,
                    ProductVariant = variant
                }
            }
        };

        _context.Products.Add(product);
        _context.ProductVariants.Add(variant);
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();

        var checkoutDto = new CreateOrderDto
        {
            ShippingCost = 10m,
            Tax = 5m,
            Discount = 0m
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/orders/{userId}/checkout",
            checkoutDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadAsAsync<Guid>();
        Assert.NotEqual(Guid.Empty, result);
    }

    [Fact]
    public async Task Checkout_ShouldReturnBadRequest_WhenCartEmpty()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var checkoutDto = new CreateOrderDto
        {
            ShippingCost = 10m,
            Tax = 5m,
            Discount = 0m
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/orders/{userId}/checkout",
            checkoutDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CancelOrder_ShouldCancelOrder_WhenOrderExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var order = new Ecommerce.Domain.Entities.Order
        {
            Id = orderId,
            UserId = userId,
            SubTotal = 100m,
            ShippingCost = 10m,
            Tax = 5m,
            Discount = 0m,
            TotalPrice = 115m,
            Status = OrderStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow,
            OrderItems = new List<Ecommerce.Domain.Entities.OrderItem>()
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Act
        var response = await _client.PutAsync($"/api/orders/{userId}/{orderId}/cancel", null);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task CancelOrder_ShouldReturnBadRequest_WhenOrderDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        // Act
        var response = await _client.PutAsync($"/api/orders/{userId}/{orderId}/cancel", null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
