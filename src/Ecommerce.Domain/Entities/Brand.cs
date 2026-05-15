using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Entities;

public class Brand : BaseEntity
{
    public string Name { get; set; } = default!;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}