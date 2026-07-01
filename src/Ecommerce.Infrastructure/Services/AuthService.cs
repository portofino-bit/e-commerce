using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Ecommerce.Application.DTOs.Auth;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly JwtSettings _jwtSettings;

    public AuthService(AppDbContext context, IOptions<JwtSettings> jwtSettings)
    {
        _context = context;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto request)
    {
        var emailExists = await _context.Users.AnyAsync(x => x.Email == request.Email);

        if (emailExists)
        {
            throw new InvalidOperationException("Email is already registered.");
        }

        var user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = Role.Customer
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return await GenerateAuthResponseAsync(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == request.Email);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        return await GenerateAuthResponseAsync(user);
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(
            x => x.RefreshToken == request.RefreshToken);

        if (user is null || user.RefreshTokenExpiryUtc < DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");
        }

        return await GenerateAuthResponseAsync(user);
    }

    public async Task LogoutAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);

        if (user is not null)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiryUtc = null;
            await _context.SaveChangesAsync();
        }
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto request)
    {
        var user = await _context.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Current password is incorrect.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _context.SaveChangesAsync();
    }

    public async Task<AuthResponseDto> GetCurrentUserAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        return MapToAuthResponse(user);
    }

    private async Task<AuthResponseDto> GenerateAuthResponseAsync(User user)
    {
        var token = GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryUtc = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);

        await _context.SaveChangesAsync();

        return new AuthResponseDto
        {
            UserId = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Token = token,
            RefreshToken = refreshToken
        };
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.GivenName, user.FirstName),
            new Claim(ClaimTypes.Surname, user.LastName),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private static AuthResponseDto MapToAuthResponse(User user)
    {
        return new AuthResponseDto
        {
            UserId = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Token = string.Empty,
            RefreshToken = string.Empty
        };
    }
}