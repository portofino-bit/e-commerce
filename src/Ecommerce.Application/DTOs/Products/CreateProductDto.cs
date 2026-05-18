namespace Ecommerce.Application.DTOs.Products;

public class CreateProductDto
{
    public string Name { get; set; } = default!;

    public string Slug { get; set; } = default!;

    public string Description { get; set; } = default!;

    public Guid CategoryId { get; set; }

    public Guid BrandId { get; set; }

    public bool IsPublished { get; set; }

    public List<CreateProductVariantDto> Variants { get; set; } = [];
}