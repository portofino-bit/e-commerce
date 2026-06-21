using Ecommerce.Application.DTOs.Orders;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;

namespace Ecommerce.UnitTests.Builders;

public class TestDataBuilder
{
    private Guid _productId = Guid.NewGuid();
    private Guid _variantId = Guid.NewGuid();
    private Guid _userId = Guid.NewGuid();
    private Guid _orderId = Guid.NewGuid();
    private string _productName = "Test Product";
    private string _productSlug = "test-product";
    private string _productDescription = "Test";
    private decimal _price = 100m;
    private int _stockQuantity = 10;
    private string _sku = "TEST-001";
    private decimal _shippingCost = 10m;
    private decimal _tax = 5m;
    private decimal _discount = 0m;
    private int _quantity = 1;
    private decimal _subTotal = 100m;
    private decimal _totalPrice = 115m;
    private OrderStatus _orderStatus = OrderStatus.Pending;
    private bool _isPublished = true;

    public TestDataBuilder WithProductId(Guid id)
    {
        _productId = id;
        return this;
    }

    public TestDataBuilder WithVariantId(Guid id)
    {
        _variantId = id;
        return this;
    }

    public TestDataBuilder WithUserId(Guid id)
    {
        _userId = id;
        return this;
    }

    public TestDataBuilder WithProductName(string name)
    {
        _productName = name;
        return this;
    }

    public TestDataBuilder WithProductSlug(string slug)
    {
        _productSlug = slug;
        return this;
    }

    public TestDataBuilder WithProductDescription(string description)
    {
        _productDescription = description;
        return this;
    }

    public TestDataBuilder WithPrice(decimal price)
    {
        _price = price;
        return this;
    }

    public TestDataBuilder WithStockQuantity(int quantity)
    {
        _stockQuantity = quantity;
        return this;
    }

    public TestDataBuilder WithSku(string sku)
    {
        _sku = sku;
        return this;
    }

    public TestDataBuilder WithShippingCost(decimal cost)
    {
        _shippingCost = cost;
        return this;
    }

    public TestDataBuilder WithTax(decimal tax)
    {
        _tax = tax;
        return this;
    }

    public TestDataBuilder WithDiscount(decimal discount)
    {
        _discount = discount;
        return this;
    }

    public TestDataBuilder WithQuantity(int qty)
    {
        _quantity = qty;
        return this;
    }

    public TestDataBuilder WithOrderId(Guid id)
    {
        _orderId = id;
        return this;
    }

    public TestDataBuilder WithSubTotal(decimal subTotal)
    {
        _subTotal = subTotal;
        return this;
    }

    public TestDataBuilder WithTotalPrice(decimal totalPrice)
    {
        _totalPrice = totalPrice;
        return this;
    }

    public TestDataBuilder WithOrderStatus(OrderStatus status)
    {
        _orderStatus = status;
        return this;
    }

    public TestDataBuilder WithIsPublished(bool isPublished)
    {
        _isPublished = isPublished;
        return this;
    }

    public Product BuildProduct()
    {
        return new Product
        {
            Id = _productId,
            Name = _productName,
            Slug = _productSlug,
            Description = _productDescription,
            CategoryId = Guid.NewGuid(),
            BrandId = Guid.NewGuid(),
            IsPublished = _isPublished
        };
    }

    public ProductVariant BuildVariant(Product product)
    {
        return new ProductVariant
        {
            Id = _variantId,
            SKU = _sku,
            Price = _price,
            StockQuantity = _stockQuantity,
            ProductId = product.Id,
            Product = product
        };
    }

    public Cart BuildCart(Product product, ProductVariant variant)
    {
        var totalPrice = _price * _quantity;
        return new Cart
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            TotalPrice = totalPrice,
            CartItems = new List<CartItem>
            {
                new CartItem
                {
                    Id = Guid.NewGuid(),
                    ProductVariantId = variant.Id,
                    Quantity = _quantity,
                    UnitPriceSnapshot = _price,
                    ProductVariant = variant
                }
            }
        };
    }

    public CreateOrderDto BuildCreateOrderDto()
    {
        return new CreateOrderDto
        {
            ShippingCost = _shippingCost,
            Tax = _tax,
            Discount = _discount
        };
    }

    public (Product product, ProductVariant variant, Cart cart, CreateOrderDto dto) BuildAllForCheckout()
    {
        var product = BuildProduct();
        var variant = BuildVariant(product);
        var cart = BuildCart(product, variant);
        var dto = BuildCreateOrderDto();
        return (product, variant, cart, dto);
    }

    public Order BuildOrder()
    {
        return new Order
        {
            Id = _orderId,
            UserId = _userId,
            SubTotal = _subTotal,
            ShippingCost = _shippingCost,
            Tax = _tax,
            Discount = _discount,
            TotalPrice = _totalPrice,
            Status = _orderStatus,
            CreatedAtUtc = DateTime.UtcNow,
            OrderItems = new List<OrderItem>()
        };
    }

    public Cart BuildEmptyCart()
    {
        return new Cart
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            TotalPrice = 0m,
            CartItems = new List<CartItem>()
        };
    }

    public Cart BuildCartWithMultipleItems(List<(ProductVariant variant, int quantity, decimal unitPrice)> items)
    {
        var cartItems = items.Select(item => new CartItem
        {
            Id = Guid.NewGuid(),
            ProductVariantId = item.variant.Id,
            Quantity = item.quantity,
            UnitPriceSnapshot = item.unitPrice,
            ProductVariant = item.variant
        }).ToList();

        return new Cart
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            TotalPrice = cartItems.Sum(ci => ci.TotalPrice),
            CartItems = cartItems
        };
    }
}