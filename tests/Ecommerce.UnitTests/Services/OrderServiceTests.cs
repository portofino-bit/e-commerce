using Ecommerce.Application.DTOs.Orders;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Infrastructure.Services;
using Ecommerce.UnitTests.Builders;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.UnitTests.Services;

public class OrderServiceTests
{
    private AppDbContext _context = null!;
    private IOrderService _orderService = null!;

    private AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task CheckoutAsync_ShouldCreateOrder_WhenValidCartExists()
    {
        // Arrange
        _context = CreateInMemoryContext();
        
        var userId = Guid.NewGuid();
        var builder = new TestDataBuilder().WithUserId(userId);
        var (product, variant, cart, createOrderDto) = builder.BuildAllForCheckout();

        _context.Products.Add(product);
        _context.ProductVariants.Add(variant);
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();
        
        _orderService = new OrderService(_context);

        // Act
        var result = await _orderService.CheckoutAsync(userId, createOrderDto);

        // Assert
        Assert.NotEqual(Guid.Empty, result);
        var createdOrder = await _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == result);
        Assert.NotNull(createdOrder);
        Assert.Equal(userId, createdOrder.UserId);
        Assert.Equal(OrderStatus.Pending, createdOrder.Status);
        Assert.Single(createdOrder.OrderItems);
        Assert.Equal(100m, createdOrder.SubTotal);
        Assert.Equal(10m, createdOrder.ShippingCost);
        Assert.Equal(5m, createdOrder.Tax);
        Assert.Equal(0m, createdOrder.Discount);
        Assert.Equal(115m, createdOrder.TotalPrice); // 100 + 10 + 5 - 0
    }

    [Fact]
    public async Task CheckoutAsync_ShouldThrowInvalidOperationException_WhenCartEmpty()
    {
        // Arrange
        _context = CreateInMemoryContext();
        await _context.Database.EnsureCreatedAsync();
        
        var userId = Guid.NewGuid();

        var createOrderDto = new CreateOrderDto
        {
            ShippingCost = 10m,
            Tax = 5m,
            Discount = 0m
        };

        _orderService = new OrderService(_context);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _orderService.CheckoutAsync(userId, createOrderDto));
    }

    [Fact]
    public async Task CheckoutAsync_ShouldThrowInvalidOperationException_WhenInsufficientStock()
    {
        // Arrange
        _context = CreateInMemoryContext();
        
        var userId = Guid.NewGuid();
        var builder = new TestDataBuilder()
            .WithUserId(userId)
            .WithStockQuantity(5)
            .WithQuantity(10);
        var (product, variant, cart, createOrderDto) = builder.BuildAllForCheckout();

        _context.Products.Add(product);
        _context.ProductVariants.Add(variant);
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();
        
        _orderService = new OrderService(_context);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _orderService.CheckoutAsync(userId, createOrderDto));
    }

    [Fact]
    public async Task CheckoutAsync_ShouldCalculateTotalPriceCorrectly_WithDiscount()
    {
        // Arrange
        _context = CreateInMemoryContext();
        
        var userId = Guid.NewGuid();
        var builder = new TestDataBuilder()
            .WithUserId(userId)
            .WithDiscount(15m);
        var (product, variant, cart, createOrderDto) = builder.BuildAllForCheckout();

        _context.Products.Add(product);
        _context.ProductVariants.Add(variant);
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();
        
        _orderService = new OrderService(_context);

        // Act
        var result = await _orderService.CheckoutAsync(userId, createOrderDto);

        // Assert
        var createdOrder = await _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == result);
        Assert.NotNull(createdOrder);
        Assert.Equal(100m, createdOrder.Discount);
        Assert.Equal(100m, createdOrder.TotalPrice); // 100 + 10 + 5 - 15
    }

    [Fact]
    public async Task GetAllForUserAsync_ShouldReturnAllOrders_WhenUserHasOrders()
    {
        // Arrange
        _context = CreateInMemoryContext();
        
        var userId = Guid.NewGuid();
        var builder1 = new TestDataBuilder()
            .WithUserId(userId)
            .WithSubTotal(100m)
            .WithTotalPrice(115m)
            .WithOrderStatus(OrderStatus.Pending);
        
        var builder2 = new TestDataBuilder()
            .WithUserId(userId)
            .WithSubTotal(200m)
            .WithShippingCost(10m)
            .WithTax(10m)
            .WithTotalPrice(220m)
            .WithOrderStatus(OrderStatus.Completed);

        var orders = new List<Order>
        {
            builder1.BuildOrder(),
            builder2.BuildOrder()
        };

        _context.Orders.AddRange(orders);
        await _context.SaveChangesAsync();
        
        _orderService = new OrderService(_context);

        // Act
        var result = await _orderService.GetAllForUserAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllForUserAsync_ShouldReturnEmptyList_WhenUserHasNoOrders()
    {
        // Arrange
        _context = CreateInMemoryContext();
        await _context.Database.EnsureCreatedAsync();
        
        var userId = Guid.NewGuid();
        _orderService = new OrderService(_context);

        // Act
        var result = await _orderService.GetAllForUserAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnOrder_WhenOrderExists()
    {
        // Arrange
        _context = CreateInMemoryContext();
        
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var builder = new TestDataBuilder()
            .WithUserId(userId)
            .WithOrderId(orderId)
            .WithTotalPrice(115m);
        var order = builder.BuildOrder();

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        
        _orderService = new OrderService(_context);

        // Act
        var result = await _orderService.GetByIdAsync(userId, orderId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(orderId, result.Id);
        Assert.Equal(115m, result.TotalPrice);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenOrderDoesNotExist()
    {
        // Arrange
        _context = CreateInMemoryContext();
        await _context.Database.EnsureCreatedAsync();
        
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        _orderService = new OrderService(_context);

        // Act
        var result = await _orderService.GetByIdAsync(userId, orderId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CancelAsync_ShouldCancelOrder_WhenOrderExists()
    {
        // Arrange
        _context = CreateInMemoryContext();
        
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var builder = new TestDataBuilder()
            .WithUserId(userId)
            .WithOrderId(orderId)
            .WithOrderStatus(OrderStatus.Pending);
        var order = builder.BuildOrder();

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        
        _orderService = new OrderService(_context);

        // Act
        await _orderService.CancelAsync(userId, orderId);

        // Assert
        var cancelledOrder = await _context.Orders.FindAsync(orderId);
        Assert.NotNull(cancelledOrder);
        Assert.Equal(OrderStatus.Cancelled, cancelledOrder.Status);
    }

    [Fact]
    public async Task CancelAsync_ShouldThrowInvalidOperationException_WhenOrderDoesNotExist()
    {
        // Arrange
        _context = CreateInMemoryContext();
        await _context.Database.EnsureCreatedAsync();
        
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        _orderService = new OrderService(_context);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _orderService.CancelAsync(userId, orderId));
    }

    [Fact]
    public async Task CheckoutAsync_ShouldCreateOrderWithMultipleItems_WhenCartHasMultipleItems()
    {
        // Arrange
        _context = CreateInMemoryContext();
        
        var userId = Guid.NewGuid();
        
        var builder1 = new TestDataBuilder()
            .WithProductName("Product 1")
            .WithSku("SKU-001")
            .WithPrice(100m)
            .WithStockQuantity(10);
        var product1 = builder1.BuildProduct();
        var variant1 = builder1.BuildVariant(product1);

        var builder2 = new TestDataBuilder()
            .WithProductName("Product 2")
            .WithSku("SKU-002")
            .WithPrice(50m)
            .WithStockQuantity(20);
        var product2 = builder2.BuildProduct();
        var variant2 = builder2.BuildVariant(product2);

        var builder = new TestDataBuilder()
            .WithUserId(userId)
            .WithShippingCost(15m)
            .WithTax(20m)
            .WithDiscount(5m);
        
        var cart = builder.BuildCartWithMultipleItems(new List<(ProductVariant, int, decimal)>
        {
            (variant1, 1, 100m),
            (variant2, 2, 50m)
        });
        var createOrderDto = builder.BuildCreateOrderDto();

        _context.Products.AddRange(product1, product2);
        _context.ProductVariants.AddRange(variant1, variant2);
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();
        
        _orderService = new OrderService(_context);

        // Act
        var result = await _orderService.CheckoutAsync(userId, createOrderDto);

        // Assert
        var createdOrder = await _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == result);
        Assert.NotNull(createdOrder);
        Assert.Equal(2, createdOrder.OrderItems.Count);
        Assert.Equal(200m, createdOrder.SubTotal);
        Assert.Equal(230m, createdOrder.TotalPrice); // 200 + 15 + 20 - 5
    }

    [Fact]
    public async Task CheckoutAsync_ShouldReduceStockQuantity_AfterCheckout()
    {
        // Arrange
        _context = CreateInMemoryContext();
        
        var userId = Guid.NewGuid();
        var builder = new TestDataBuilder()
            .WithUserId(userId)
            .WithQuantity(3);
        var (product, variant, cart, createOrderDto) = builder.BuildAllForCheckout();

        _context.Products.Add(product);
        _context.ProductVariants.Add(variant);
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();

        var initialStock = variant.StockQuantity;
        var variantId = variant.Id;
        
        _orderService = new OrderService(_context);

        // Act
        await _orderService.CheckoutAsync(userId, createOrderDto);

        // Assert
        var updatedVariant = await _context.ProductVariants.FindAsync(variantId);
        Assert.NotNull(updatedVariant);
        Assert.Equal(initialStock - 3, updatedVariant.StockQuantity);
        Assert.Equal(7, updatedVariant.StockQuantity);
    }

    [Fact]
    public async Task CheckoutAsync_ShouldClearCart_AfterSuccessfulCheckout()
    {
        // Arrange
        _context = CreateInMemoryContext();
        
        var userId = Guid.NewGuid();
        var builder = new TestDataBuilder().WithUserId(userId);
        var (product, variant, cart, createOrderDto) = builder.BuildAllForCheckout();
        var cartId = cart.Id;

        _context.Products.Add(product);
        _context.ProductVariants.Add(variant);
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();
        
        _orderService = new OrderService(_context);

        // Act
        await _orderService.CheckoutAsync(userId, createOrderDto);

        // Assert
        var clearedCart = await _context.Carts.Include(c => c.CartItems).FirstOrDefaultAsync(c => c.Id == cartId);
        Assert.NotNull(clearedCart);
        Assert.Empty(clearedCart.CartItems);
    }

    [Fact]
    public async Task CancelAsync_ShouldThrowInvalidOperationException_WhenCancellingCompletedOrder()
    {
        // Arrange
        _context = CreateInMemoryContext();
        
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var builder = new TestDataBuilder()
            .WithUserId(userId)
            .WithOrderId(orderId)
            .WithOrderStatus(OrderStatus.Shipped)
            .WithTotalPrice(115m);
        var order = builder.BuildOrder();

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        
        _orderService = new OrderService(_context);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _orderService.CancelAsync(userId, orderId));
    }

    [Fact]
    public async Task CancelAsync_ShouldThrowInvalidOperationException_WhenCancellingAlreadyCancelledOrder()
    {
        // Arrange
        _context = CreateInMemoryContext();
        
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var builder = new TestDataBuilder()
            .WithUserId(userId)
            .WithOrderId(orderId)
            .WithOrderStatus(OrderStatus.Cancelled)
            .WithTotalPrice(115m);
        var order = builder.BuildOrder();

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        
        _orderService = new OrderService(_context);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _orderService.CancelAsync(userId, orderId));
    }

    [Fact]
    public async Task CheckoutAsync_ShouldThrowInvalidOperationException_WhenDiscountIsNegative()
    {
        // Arrange
        _context = CreateInMemoryContext();
        
        var userId = Guid.NewGuid();
        var builder = new TestDataBuilder()
            .WithUserId(userId)
            .WithDiscount(-10m);
        var (product, variant, cart, createOrderDto) = builder.BuildAllForCheckout();

        _context.Products.Add(product);
        _context.ProductVariants.Add(variant);
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();
        
        _orderService = new OrderService(_context);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _orderService.CheckoutAsync(userId, createOrderDto));
    }

    [Fact]
    public async Task CheckoutAsync_ShouldThrowInvalidOperationException_WhenTaxIsNegative()
    {
        // Arrange
        _context = CreateInMemoryContext();
        
        var userId = Guid.NewGuid();
        var builder = new TestDataBuilder()
            .WithUserId(userId)
            .WithTax(-5m);
        var (product, variant, cart, createOrderDto) = builder.BuildAllForCheckout();

        _context.Products.Add(product);
        _context.ProductVariants.Add(variant);
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();
        
        _orderService = new OrderService(_context);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _orderService.CheckoutAsync(userId, createOrderDto));
    }

    [Fact]
    public async Task CheckoutAsync_ShouldThrowInvalidOperationException_WhenCartHasZeroQuantityItem()
    {
        // Arrange
        _context = CreateInMemoryContext();
        
        var userId = Guid.NewGuid();
        var builder = new TestDataBuilder()
            .WithUserId(userId)
            .WithQuantity(0);
        var (product, variant, cart, createOrderDto) = builder.BuildAllForCheckout();

        _context.Products.Add(product);
        _context.ProductVariants.Add(variant);
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();
        
        _orderService = new OrderService(_context);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _orderService.CheckoutAsync(userId, createOrderDto));
    }

    [Fact]
    public async Task CheckoutAsync_ShouldHandleConcurrentRequests_Independently()
    {
        // Arrange
        _context = CreateInMemoryContext();
        
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        var builder1 = new TestDataBuilder()
            .WithUserId(userId1)
            .WithPrice(100m)
            .WithStockQuantity(100)
            .WithQuantity(2);
        var (product1, variant1, cart1, createOrderDto1) = builder1.BuildAllForCheckout();

        var builder2 = new TestDataBuilder()
            .WithUserId(userId2)
            .WithPrice(50m)
            .WithStockQuantity(100)
            .WithQuantity(3)
            .WithShippingCost(15m)
            .WithTax(3m)
            .WithDiscount(5m);
        var (product2, variant2, cart2, createOrderDto2) = builder2.BuildAllForCheckout();

        _context.Products.AddRange(product1, product2);
        _context.ProductVariants.AddRange(variant1, variant2);
        _context.Carts.AddRange(cart1, cart2);
        await _context.SaveChangesAsync();
        
        _orderService = new OrderService(_context);

        // Act
        var task1 = _orderService.CheckoutAsync(userId1, createOrderDto1);
        var task2 = _orderService.CheckoutAsync(userId2, createOrderDto2);
        await Task.WhenAll(task1, task2);

        var orderId1 = await task1;
        var orderId2 = await task2;

        // Assert
        Assert.NotEqual(Guid.Empty, orderId1);
        Assert.NotEqual(Guid.Empty, orderId2);
        Assert.NotEqual(orderId1, orderId2);

        var order1 = await _context.Orders.FindAsync(orderId1);
        var order2 = await _context.Orders.FindAsync(orderId2);

        Assert.NotNull(order1);
        Assert.NotNull(order2);
        Assert.Equal(userId1, order1.UserId);
        Assert.Equal(userId2, order2.UserId);
        Assert.Equal(215m, order1.TotalPrice); // 200 + 10 + 5
        Assert.Equal(163m, order2.TotalPrice); // 150 + 15 + 3 - 5
    }

    // Removed CreateMockDbSet helper method - using InMemoryDatabase instead
}
