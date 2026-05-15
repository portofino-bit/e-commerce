using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = default!;

    public string Slug { get; set; } = default!;

    public string Description { get; set; } = default!;

    public Guid CategoryId { get; set; }

    public Guid BrandId { get; set; }

    public bool IsPublished { get; set; }

    public Category Category { get; set; } = default!;

    public Brand Brand { get; set; } = default!;

    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
}