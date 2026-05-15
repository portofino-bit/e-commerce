using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Entities;

public class Address : BaseEntity
{
    public Guid UserId { get; set; }

    public string Country { get; set; } = default!;

    public string City { get; set; } = default!;

    public string Street { get; set; } = default!;

    public string ZipCode { get; set; } = default!;

    public User User { get; set; } = default!;
}