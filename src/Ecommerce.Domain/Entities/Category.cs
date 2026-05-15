using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = default!;

    public Guid? ParentCategoryId { get; set; }

    public Category? ParentCategory { get; set; }

    public ICollection<Category> Children { get; set; } = new List<Category>();

    public ICollection<Product> Products { get; set; } = new List<Product>();
}