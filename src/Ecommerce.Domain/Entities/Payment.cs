using Ecommerce.Domain.Common;
using Ecommerce.Domain.Enums;

namespace Ecommerce.Domain.Entities;

public class Payment : BaseEntity
{
    public Guid OrderId { get; set; }

    public string Provider { get; set; } = default!;

    public string TransactionId { get; set; } = default!;

    public decimal Amount { get; set; }

    public PaymentStatus Status { get; set; }

    public Order Order { get; set; } = default!;
}