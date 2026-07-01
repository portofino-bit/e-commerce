namespace Ecommerce.Application.DTOs.Auth;

public class AuthResponseDto
{
    public Guid UserId { get; set; }

    public string FirstName { get; set; } = default!;

    public string LastName { get; set; } = default!;

    public string Email { get; set; } = default!;

    public string Token { get; set; } = default!;

    public string RefreshToken { get; set; } = default!;
}