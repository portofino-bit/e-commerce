using Ecommerce.Application.DTOs.Products;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Services;

public class ProductService : IProductService
{
    private readonly AppDbContext _context;

    public ProductService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductResponseDto>> GetAllAsync(
        string? search,
        Guid? categoryId,
        string? sortBy,
        int page,
        int pageSize)
    {
        var query = _context.Products
            .Include(x => x.Category)
            .Include(x => x.Brand)
            .Include(x => x.Variants)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                x.Name.ToLower().Contains(search.ToLower()));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == categoryId.Value);
        }

        query = sortBy?.ToLower() switch
        {
            "name" => query.OrderBy(x => x.Name),
            "price_desc" => query.OrderByDescending(
                x => x.Variants.Min(v => v.Price)),
            "price_asc" => query.OrderBy(
                x => x.Variants.Min(v => v.Price)),
            _ => query.OrderByDescending(x => x.CreatedAtUtc)
        };

        query = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        var products = await query.ToListAsync();

        return products.Select(x => new ProductResponseDto
        {
            Id = x.Id,
            Name = x.Name,
            Slug = x.Slug,
            Description = x.Description,
            CategoryName = x.Category.Name,
            BrandName = x.Brand.Name,
            IsPublished = x.IsPublished,

            Variants = x.Variants.Select(v =>
                new ProductVariantDto
                {
                    Id = v.Id,
                    SKU = v.SKU,
                    Color = v.Color,
                    Size = v.Size,
                    Price = v.Price,
                    StockQuantity = v.StockQuantity
                }).ToList()
        }).ToList();
    }

    public async Task<ProductResponseDto?> GetByIdAsync(Guid id)
    {
        var product = await _context.Products
            .Include(x => x.Category)
            .Include(x => x.Brand)
            .Include(x => x.Variants)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (product is null)
        {
            return null;
        }

        return new ProductResponseDto
        {
            Id = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            Description = product.Description,
            CategoryName = product.Category.Name,
            BrandName = product.Brand.Name,
            IsPublished = product.IsPublished,

            Variants = product.Variants.Select(v =>
                new ProductVariantDto
                {
                    Id = v.Id,
                    SKU = v.SKU,
                    Color = v.Color,
                    Size = v.Size,
                    Price = v.Price,
                    StockQuantity = v.StockQuantity
                }).ToList()
        };
    }

    public async Task<Guid> CreateAsync(CreateProductDto request)
    {
        var product = new Product
        {
            Name = request.Name,
            Slug = request.Slug,
            Description = request.Description,
            CategoryId = request.CategoryId,
            BrandId = request.BrandId,
            IsPublished = request.IsPublished,

            Variants = request.Variants.Select(v =>
                new ProductVariant
                {
                    SKU = v.SKU,
                    Color = v.Color,
                    Size = v.Size,
                    Price = v.Price,
                    StockQuantity = v.StockQuantity
                }).ToList()
        };

        _context.Products.Add(product);

        await _context.SaveChangesAsync();

        return product.Id;
    }

    public async Task UpdateAsync(Guid id, CreateProductDto request)
    {
        var product = await _context.Products
            .Include(x => x.Variants)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (product is null)
        {
            throw new Exception("Product not found");
        }

        product.Name = request.Name;
        product.Description = request.Description;
        product.Slug = request.Slug;
        product.CategoryId = request.CategoryId;
        product.BrandId = request.BrandId;
        product.IsPublished = request.IsPublished;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(x => x.Id == id);

        if (product is null)
        {
            throw new Exception("Product not found");
        }

        product.IsDeleted = true;

        await _context.SaveChangesAsync();
    }
}