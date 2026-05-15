using Ecommerce.Domain.Common;
using Ecommerce.Domain.Enums;

namespace Ecommerce.Domain.Entities;

public class Order : BaseEntity
{
    public Guid UserId { get; set; }

    public decimal SubTotal { get; set; }

    public decimal ShippingCost { get; set; }

    public decimal Tax { get; set; }

    public decimal Discount { get; set; }

    public decimal TotalPrice { get; set; }

    public OrderStatus Status { get; set; }

    public User User { get; set; } = default!;

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}