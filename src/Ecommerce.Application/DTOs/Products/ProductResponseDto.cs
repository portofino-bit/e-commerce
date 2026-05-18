namespace Ecommerce.Application.DTOs.Products;

public class ProductResponseDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = default!;

    public string Slug { get; set; } = default!;

    public string Description { get; set; } = default!;

    public string CategoryName { get; set; } = default!;

    public string BrandName { get; set; } = default!;

    public bool IsPublished { get; set; }

    public List<ProductVariantDto> Variants { get; set; } = [];
}