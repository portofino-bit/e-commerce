using System.Net;
using System.Net.Http.Json;
using Ecommerce.Application.DTOs.Products;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.IntegrationTests.Controllers;

public class ProductsControllerIntegrationTests : IAsyncLifetime
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
    public async Task GetAll_ShouldReturnOkWithProducts()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var brandId = Guid.NewGuid();

        var category = new Ecommerce.Domain.Entities.Category
        {
            Id = categoryId,
            Name = "Electronics"
        };

        var brand = new Ecommerce.Domain.Entities.Brand
        {
            Id = brandId,
            Name = "TechBrand"
        };

        var product = new Ecommerce.Domain.Entities.Product
        {
            Id = Guid.NewGuid(),
            Name = "Laptop",
            Slug = "laptop",
            Description = "High-end laptop",
            CategoryId = categoryId,
            BrandId = brandId,
            IsPublished = true,
            Category = category,
            Brand = brand,
            Variants = new List<Ecommerce.Domain.Entities.ProductVariant>()
        };

        _context.Categories.Add(category);
        _context.Brands.Add(brand);
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/products");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsAsync<List<ProductResponseDto>>();
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Laptop", result[0].Name);
    }

    [Fact]
    public async Task GetAll_ShouldFilterBySearch()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var brandId = Guid.NewGuid();

        var category = new Ecommerce.Domain.Entities.Category
        {
            Id = categoryId,
            Name = "Electronics"
        };

        var brand = new Ecommerce.Domain.Entities.Brand
        {
            Id = brandId,
            Name = "TechBrand"
        };

        var product1 = new Ecommerce.Domain.Entities.Product
        {
            Id = Guid.NewGuid(),
            Name = "Laptop",
            Slug = "laptop",
            Description = "High-end laptop",
            CategoryId = categoryId,
            BrandId = brandId,
            IsPublished = true,
            Category = category,
            Brand = brand,
            Variants = new List<Ecommerce.Domain.Entities.ProductVariant>()
        };

        var product2 = new Ecommerce.Domain.Entities.Product
        {
            Id = Guid.NewGuid(),
            Name = "Mouse",
            Slug = "mouse",
            Description = "Wireless mouse",
            CategoryId = categoryId,
            BrandId = brandId,
            IsPublished = true,
            Category = category,
            Brand = brand,
            Variants = new List<Ecommerce.Domain.Entities.ProductVariant>()
        };

        _context.Categories.Add(category);
        _context.Brands.Add(brand);
        _context.Products.AddRange(product1, product2);
        await _context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/products?search=Laptop");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsAsync<List<ProductResponseDto>>();
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Laptop", result[0].Name);
    }

    [Fact]
    public async Task GetById_ShouldReturnProduct_WhenExists()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var brandId = Guid.NewGuid();

        var category = new Ecommerce.Domain.Entities.Category
        {
            Id = categoryId,
            Name = "Electronics"
        };

        var brand = new Ecommerce.Domain.Entities.Brand
        {
            Id = brandId,
            Name = "TechBrand"
        };

        var product = new Ecommerce.Domain.Entities.Product
        {
            Id = productId,
            Name = "Laptop",
            Slug = "laptop",
            Description = "High-end laptop",
            CategoryId = categoryId,
            BrandId = brandId,
            IsPublished = true,
            Category = category,
            Brand = brand,
            Variants = new List<Ecommerce.Domain.Entities.ProductVariant>()
        };

        _context.Categories.Add(category);
        _context.Brands.Add(brand);
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/products/{productId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsAsync<ProductResponseDto>();
        Assert.NotNull(result);
        Assert.Equal(productId, result.Id);
        Assert.Equal("Laptop", result.Name);
        Assert.Equal("High-end laptop", result.Description);
        Assert.Equal(categoryId, result.CategoryId);
        Assert.Equal(brandId, result.BrandId);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenProductDoesNotExist()
    {
        // Act
        var response = await _client.GetAsync($"/api/products/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_ShouldCreateProduct_WhenValidDataProvided()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var brandId = Guid.NewGuid();

        var category = new Ecommerce.Domain.Entities.Category
        {
            Id = categoryId,
            Name = "Electronics"
        };

        var brand = new Ecommerce.Domain.Entities.Brand
        {
            Id = brandId,
            Name = "TechBrand"
        };

        _context.Categories.Add(category);
        _context.Brands.Add(brand);
        await _context.SaveChangesAsync();

        var createDto = new CreateProductDto
        {
            Name = "New Laptop",
            Slug = "new-laptop",
            Description = "Brand new laptop",
            CategoryId = categoryId,
            BrandId = brandId,
            IsPublished = true,
            Variants = new List<CreateProductVariantDto>()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var productId = await response.Content.ReadAsAsync<Guid>();
        Assert.NotEqual(Guid.Empty, productId);

        var created =
            await _context.Products.FindAsync(productId);

        Assert.NotNull(created);
    }

    [Fact]
    public async Task Update_ShouldUpdateProduct_WhenProductExists()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var brandId = Guid.NewGuid();

        var category = new Ecommerce.Domain.Entities.Category
        {
            Id = categoryId,
            Name = "Electronics"
        };

        var brand = new Ecommerce.Domain.Entities.Brand
        {
            Id = brandId,
            Name = "TechBrand"
        };

        var product = new Ecommerce.Domain.Entities.Product
        {
            Id = productId,
            Name = "Old Laptop",
            Slug = "old-laptop",
            Description = "Old description",
            CategoryId = categoryId,
            BrandId = brandId,
            IsPublished = false,
            Category = category,
            Brand = brand,
            Variants = new List<Ecommerce.Domain.Entities.ProductVariant>()
        };

        _context.Categories.Add(category);
        _context.Brands.Add(brand);
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var updateDto = new CreateProductDto
        {
            Name = "Updated Laptop",
            Slug = "updated-laptop",
            Description = "Updated description",
            CategoryId = categoryId,
            BrandId = brandId,
            IsPublished = true,
            Variants = new List<CreateProductVariantDto>()
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/products/{productId}", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var updated = await _context.Products.FindAsync(productId);
        Assert.Equal("Updated Laptop", updated.Name);
    }

    [Fact]
    public async Task Delete_ShouldDeleteProduct_WhenProductExists()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var brandId = Guid.NewGuid();

        var category = new Ecommerce.Domain.Entities.Category
        {
            Id = categoryId,
            Name = "Electronics"
        };

        var brand = new Ecommerce.Domain.Entities.Brand
        {
            Id = brandId,
            Name = "TechBrand"
        };

        var product = new Ecommerce.Domain.Entities.Product
        {
            Id = productId,
            Name = "Laptop",
            Slug = "laptop",
            Description = "Description",
            CategoryId = categoryId,
            BrandId = brandId,
            IsPublished = true,
            Category = category,
            Brand = brand,
            Variants = new List<Ecommerce.Domain.Entities.ProductVariant>()
        };

        _context.Categories.Add(category);
        _context.Brands.Add(brand);
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/products/{productId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var deleted = await _context.Products.FindAsync(productId);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task GetAll_SearchNotFound_ShouldReturnEmptyResult()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var brandId = Guid.NewGuid();

        var category = new Ecommerce.Domain.Entities.Category
        {
            Id = categoryId,
            Name = "Electronics"
        };

        var brand = new Ecommerce.Domain.Entities.Brand
        {
            Id = brandId,
            Name = "TechBrand"
        };

        var product = new Ecommerce.Domain.Entities.Product
        {
            Id = Guid.NewGuid(),
            Name = "Laptop",
            Slug = "laptop",
            Description = "High-end laptop",
            CategoryId = categoryId,
            BrandId = brandId,
            IsPublished = true,
            Category = category,
            Brand = brand,
            Variants = new List<Ecommerce.Domain.Entities.ProductVariant>()
        };

        _context.Categories.Add(category);
        _context.Brands.Add(brand);
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/products?search=xyz");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsAsync<List<ProductResponseDto>>();
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAll_InvalidPage_ShouldReturnValidationException()
    {
        // Act
        var response = await _client.GetAsync("/api/products?page=0");

        // Assert
        // Invalid input MUST be rejected
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_ProductNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistingProductId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var brandId = Guid.NewGuid();

        var category = new Ecommerce.Domain.Entities.Category
        {
            Id = categoryId,
            Name = "Electronics"
        };

        var brand = new Ecommerce.Domain.Entities.Brand
        {
            Id = brandId,
            Name = "TechBrand"
        };

        _context.Categories.Add(category);
        _context.Brands.Add(brand);
        await _context.SaveChangesAsync();

        var updateDto = new CreateProductDto
        {
            Name = "Updated Laptop",
            Slug = "updated-laptop",
            Description = "Updated description",
            CategoryId = categoryId,
            BrandId = brandId,
            IsPublished = true,
            Variants = new List<CreateProductVariantDto>()
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/products/{nonExistingProductId}", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ProductNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistingProductId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/api/products/{nonExistingProductId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_DuplicateSlug_ShouldReturnConflict()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var existingSlug = "existing-laptop";

        var category = new Ecommerce.Domain.Entities.Category
        {
            Id = categoryId,
            Name = "Electronics"
        };

        var brand = new Ecommerce.Domain.Entities.Brand
        {
            Id = brandId,
            Name = "TechBrand"
        };

        var existingProduct = new Ecommerce.Domain.Entities.Product
        {
            Id = Guid.NewGuid(),
            Name = "Existing Laptop",
            Slug = existingSlug,
            Description = "Existing laptop",
            CategoryId = categoryId,
            BrandId = brandId,
            IsPublished = true,
            Category = category,
            Brand = brand,
            Variants = new List<Ecommerce.Domain.Entities.ProductVariant>()
        };

        _context.Categories.Add(category);
        _context.Brands.Add(brand);
        _context.Products.Add(existingProduct);
        await _context.SaveChangesAsync();

        var createDto = new CreateProductDto
        {
            Name = "New Laptop",
            Slug = existingSlug,
            Description = "New laptop with duplicate slug",
            CategoryId = categoryId,
            BrandId = brandId,
            IsPublished = true,
            Variants = new List<CreateProductVariantDto>()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_SortByNameAscending_ShouldReturnSortedResults()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var brandId = Guid.NewGuid();

        var category = new Ecommerce.Domain.Entities.Category
        {
            Id = categoryId,
            Name = "Electronics"
        };

        var brand = new Ecommerce.Domain.Entities.Brand
        {
            Id = brandId,
            Name = "TechBrand"
        };

        var products = new List<Ecommerce.Domain.Entities.Product>
        {
            new Ecommerce.Domain.Entities.Product
            {
                Id = Guid.NewGuid(),
                Name = "Zebra Product",
                Slug = "zebra-product",
                Description = "Z",
                CategoryId = categoryId,
                BrandId = brandId,
                IsPublished = true,
                Category = category,
                Brand = brand,
                Variants = new List<Ecommerce.Domain.Entities.ProductVariant>()
            },
            new Ecommerce.Domain.Entities.Product
            {
                Id = Guid.NewGuid(),
                Name = "Apple Product",
                Slug = "apple-product",
                Description = "A",
                CategoryId = categoryId,
                BrandId = brandId,
                IsPublished = true,
                Category = category,
                Brand = brand,
                Variants = new List<Ecommerce.Domain.Entities.ProductVariant>()
            }
        };

        _context.Categories.Add(category);
        _context.Brands.Add(brand);
        _context.Products.AddRange(products);
        await _context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/products?sortBy=name_asc");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsAsync<List<ProductResponseDto>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Apple Product", result[0].Name);
        Assert.Equal("Zebra Product", result[1].Name);
    }

    [Fact]
    public async Task GetAll_SortByNameDescending_ShouldReturnSortedResults()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var brandId = Guid.NewGuid();

        var category = new Ecommerce.Domain.Entities.Category
        {
            Id = categoryId,
            Name = "Electronics"
        };

        var brand = new Ecommerce.Domain.Entities.Brand
        {
            Id = brandId,
            Name = "TechBrand"
        };

        var products = new List<Ecommerce.Domain.Entities.Product>
        {
            new Ecommerce.Domain.Entities.Product
            {
                Id = Guid.NewGuid(),
                Name = "Zebra Product",
                Slug = "zebra-product",
                Description = "Z",
                CategoryId = categoryId,
                BrandId = brandId,
                IsPublished = true,
                Category = category,
                Brand = brand,
                Variants = new List<Ecommerce.Domain.Entities.ProductVariant>()
            },
            new Ecommerce.Domain.Entities.Product
            {
                Id = Guid.NewGuid(),
                Name = "Apple Product",
                Slug = "apple-product",
                Description = "A",
                CategoryId = categoryId,
                BrandId = brandId,
                IsPublished = true,
                Category = category,
                Brand = brand,
                Variants = new List<Ecommerce.Domain.Entities.ProductVariant>()
            }
        };

        _context.Categories.Add(category);
        _context.Brands.Add(brand);
        _context.Products.AddRange(products);
        await _context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/products?sortBy=name_desc");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsAsync<List<ProductResponseDto>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Zebra Product", result[0].Name);
        Assert.Equal("Apple Product", result[1].Name);
    }

    [Fact]
    public async Task GetAll_SortByPriceAscending_ShouldReturnSortedResults()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var brandId = Guid.NewGuid();

        var category = new Ecommerce.Domain.Entities.Category
        {
            Id = categoryId,
            Name = "Electronics"
        };

        var brand = new Ecommerce.Domain.Entities.Brand
        {
            Id = brandId,
            Name = "TechBrand"
        };

        var products = new List<Ecommerce.Domain.Entities.Product>
        {
            new Ecommerce.Domain.Entities.Product
            {
                Id = Guid.NewGuid(),
                Name = "Expensive Product",
                Slug = "expensive-product",
                Description = "High price",
                CategoryId = categoryId,
                BrandId = brandId,
                IsPublished = true,
                Category = category,
                Brand = brand,
                Variants = new List<Ecommerce.Domain.Entities.ProductVariant>
                {
                    new Ecommerce.Domain.Entities.ProductVariant
                    {
                        Id = Guid.NewGuid(),
                        Price = 999.99m
                    }
                }
            },
            new Ecommerce.Domain.Entities.Product
            {
                Id = Guid.NewGuid(),
                Name = "Cheap Product",
                Slug = "cheap-product",
                Description = "Low price",
                CategoryId = categoryId,
                BrandId = brandId,
                IsPublished = true,
                Category = category,
                Brand = brand,
                Variants = new List<Ecommerce.Domain.Entities.ProductVariant>
                {
                    new Ecommerce.Domain.Entities.ProductVariant
                    {
                        Id = Guid.NewGuid(),
                        Price = 10.99m
                    }
                }
            }
        };

        _context.Categories.Add(category);
        _context.Brands.Add(brand);
        _context.Products.AddRange(products);
        await _context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/products?sortBy=price_asc");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsAsync<List<ProductResponseDto>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Cheap Product", result[0].Name);
        Assert.Equal("Expensive Product", result[1].Name);
    }

    [Fact]
    public async Task GetAll_SortByPriceDescending_ShouldReturnSortedResults()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var brandId = Guid.NewGuid();

        var category = new Ecommerce.Domain.Entities.Category
        {
            Id = categoryId,
            Name = "Electronics"
        };

        var brand = new Ecommerce.Domain.Entities.Brand
        {
            Id = brandId,
            Name = "TechBrand"
        };

        var products = new List<Ecommerce.Domain.Entities.Product>
        {
            new Ecommerce.Domain.Entities.Product
            {
                Id = Guid.NewGuid(),
                Name = "Expensive Product",
                Slug = "expensive-product",
                Description = "High price",
                CategoryId = categoryId,
                BrandId = brandId,
                IsPublished = true,
                Category = category,
                Brand = brand,
                Variants = new List<Ecommerce.Domain.Entities.ProductVariant>
                {
                    new Ecommerce.Domain.Entities.ProductVariant
                    {
                        Id = Guid.NewGuid(),
                        Price = 999.99m
                    }
                }
            },
            new Ecommerce.Domain.Entities.Product
            {
                Id = Guid.NewGuid(),
                Name = "Cheap Product",
                Slug = "cheap-product",
                Description = "Low price",
                CategoryId = categoryId,
                BrandId = brandId,
                IsPublished = true,
                Category = category,
                Brand = brand,
                Variants = new List<Ecommerce.Domain.Entities.ProductVariant>
                {
                    new Ecommerce.Domain.Entities.ProductVariant
                    {
                        Id = Guid.NewGuid(),
                        Price = 10.99m
                    }
                }
            }
        };

        _context.Categories.Add(category);
        _context.Brands.Add(brand);
        _context.Products.AddRange(products);
        await _context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/products?sortBy=price_desc");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsAsync<List<ProductResponseDto>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Expensive Product", result[0].Name);
        Assert.Equal("Cheap Product", result[1].Name);
    }
}
