using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Entities;

public class Cart : BaseEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = default!;

    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
}