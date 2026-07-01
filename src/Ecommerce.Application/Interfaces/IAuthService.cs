using Ecommerce.Application.DTOs.Auth;

namespace Ecommerce.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto request);

    Task<AuthResponseDto> LoginAsync(LoginDto request);

    Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto request);

    Task LogoutAsync(Guid userId);

    Task ChangePasswordAsync(Guid userId, ChangePasswordDto request);

    Task<AuthResponseDto> GetCurrentUserAsync(Guid userId);
}