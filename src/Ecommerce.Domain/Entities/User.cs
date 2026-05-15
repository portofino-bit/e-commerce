using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Entities;

public class User : BaseEntity
{
    public string FirstName { get; set; } = default!;

    public string LastName { get; set; } = default!;

    public string Email { get; set; } = default!;

    public string PasswordHash { get; set; } = default!;

    public string PhoneNumber { get; set; } = default!;

    public ICollection<Order> Orders { get; set; } = new List<Order>();

    public ICollection<Address> Addresses { get; set; } = new List<Address>();
}