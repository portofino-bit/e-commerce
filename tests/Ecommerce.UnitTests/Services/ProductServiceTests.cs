using Ecommerce.Application.DTOs.Products;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Infrastructure.Services;
using Ecommerce.UnitTests.Builders;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.UnitTests.Services;

public class ProductServiceTests
{
    private AppDbContext _context = null!;
    private IProductService _productService = null!;

    private AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllProducts_WhenNoFiltersApplied()
    {
        // Arrange
        _context = CreateInMemoryContext();
        
        var builder1 = new TestDataBuilder()
            .WithProductName("Product 1")
            .WithProductSlug("product-1");
        var product1 = builder1.BuildProduct();
        
        var builder2 = new TestDataBuilder()
            .WithProductName("Product 2")
            .WithProductSlug("product-2");
        var product2 = builder2.BuildProduct();

        _context.Products.AddRange(product1, product2);
        await _context.SaveChangesAsync();
        
        _productService = new ProductService(_context);

        // Act
        var result = await _productService.GetAllAsync(
            search: null,
            categoryId: null,
            sortBy: null,
            page: 1,
            pageSize: 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Product 1", result[0].Name);
        Assert.Equal("Product 2", result[1].Name);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterBySearch_WhenSearchTermProvided()
    {
        // Arrange
        _context = CreateInMemoryContext();
        
        var builder1 = new TestDataBuilder()
            .WithProductName("Laptop")
            .WithProductSlug("laptop")
            .WithProductDescription("High performance laptop");
        var product1 = builder1.BuildProduct();
        
        var builder2 = new TestDataBuilder()
            .WithProductName("Mouse")
            .WithProductSlug("mouse")
            .WithProductDescription("Wireless mouse");
        var product2 = builder2.BuildProduct();

        _context.Products.AddRange(product1, product2);
        await _context.SaveChangesAsync();
        
        _productService = new ProductService(_context);

        // Act
        var result = await _productService.GetAllAsync(
            search: "laptop",
            categoryId: null,
            sortBy: null,
            page: 1,
            pageSize: 10);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Laptop", result[0].Name);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterByCategory_WhenCategoryIdProvided()
    {
        // Arrange
        _context = CreateInMemoryContext();
        
        var categoryId = Guid.NewGuid();
        var builder1 = new TestDataBuilder()
            .WithProductName("Product 1")
            .WithProductSlug("product-1");
        var product1 = builder1.BuildProduct();
        product1.CategoryId = categoryId;
        
        var builder2 = new TestDataBuilder()
            .WithProductName("Product 2")
            .WithProductSlug("product-2");
        var product2 = builder2.BuildProduct();

        _context.Products.AddRange(product1, product2);
        await _context.SaveChangesAsync();
        
        _productService = new ProductService(_context);

        // Act
        var result = await _productService.GetAllAsync(
            search: null,
            categoryId: categoryId,
            sortBy: null,
            page: 1,
            pageSize: 10);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Product 1", result[0].Name);
    }

    [Fact]
    public async Task GetAllAsync_ShouldPaginate_WhenPageAndPageSizeProvided()
    {
        // Arrange
        _context = CreateInMemoryContext();
        
        var products = new List<Product>();
        for (int i = 1; i <= 25; i++)
        {
            var builder = new TestDataBuilder()
                .WithProductName($"Product {i}")
                .WithProductSlug($"product-{i}");
            products.Add(builder.BuildProduct());
        }

        _context.Products.AddRange(products);
        await _context.SaveChangesAsync();
        
        _productService = new ProductService(_context);

        // Act
        var result = await _productService.GetAllAsync(
            search: null,
            categoryId: null,
            sortBy: null,
            page: 2,
            pageSize: 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.Count);
        Assert.Equal("Product 11", result[0].Name);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnProduct_WhenProductExists()
    {
        // Arrange
        _context = CreateInMemoryContext();
        
        var productId = Guid.NewGuid();
        var builder = new TestDataBuilder()
            .WithProductId(productId)
            .WithProductName("Test Product")
            .WithProductSlug("test-product")
            .WithProductDescription("Test Description");
        var product = builder.BuildProduct();

        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        
        _productService = new ProductService(_context);

        // Act
        var result = await _productService.GetByIdAsync(productId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Product", result.Name);
        Assert.Equal("test-product", result.Slug);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenProductDoesNotExist()
    {
        // Arrange
        _context = CreateInMemoryContext();
        await _context.Database.EnsureCreatedAsync();
        
        _productService = new ProductService(_context);

        // Act
        var result = await _productService.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_ShouldAddProduct_WhenValidDataProvided()
    {
        // Arrange
        _context = CreateInMemoryContext();
        
        var categoryId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var createDto = new CreateProductDto
        {
            Name = "New Product",
            Slug = "new-product",
            Description = "New Description",
            CategoryId = categoryId,
            BrandId = brandId,
            IsPublished = true,
            Variants = new List<CreateProductVariantDto>()
        };

        _productService = new ProductService(_context);

        // Act
        var result = await _productService.CreateAsync(createDto);

        // Assert
        Assert.NotEqual(Guid.Empty, result);
        var createdProduct = await _context.Products.FindAsync(result);
        Assert.NotNull(createdProduct);
        Assert.Equal("New Product", createdProduct.Name);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateProduct_WhenProductExists()
    {
        // Arrange
        _context = CreateInMemoryContext();
        
        var productId = Guid.NewGuid();
        var builder = new TestDataBuilder()
            .WithProductId(productId)
            .WithProductName("Old Product")
            .WithProductSlug("old-product")
            .WithProductDescription("Old Description")
            .WithIsPublished(false);
        var product = builder.BuildProduct();

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var updateDto = new CreateProductDto
        {
            Name = "Updated Product",
            Slug = "updated-product",
            Description = "Updated Description",
            CategoryId = Guid.NewGuid(),
            BrandId = Guid.NewGuid(),
            IsPublished = true,
            Variants = new List<CreateProductVariantDto>()
        };

        _productService = new ProductService(_context);

        // Act
        await _productService.UpdateAsync(productId, updateDto);

        // Assert
        var updatedProduct = await _context.Products.FindAsync(productId);
        Assert.NotNull(updatedProduct);
        Assert.Equal("Updated Product", updatedProduct.Name);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveProduct_WhenProductExists()
    {
        // Arrange
        _context = CreateInMemoryContext();
        
        var productId = Guid.NewGuid();
        var builder = new TestDataBuilder()
            .WithProductId(productId)
            .WithProductName("Product to Delete")
            .WithProductSlug("product-to-delete")
            .WithProductDescription("Description");
        var product = builder.BuildProduct();

        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        
        _productService = new ProductService(_context);

        // Act
        await _productService.DeleteAsync(productId);

        // Assert
        var deletedProduct = await _context.Products.FindAsync(productId);
        Assert.Null(deletedProduct);
    }
}