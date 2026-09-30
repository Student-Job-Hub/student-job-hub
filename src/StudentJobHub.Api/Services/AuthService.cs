using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StudentJobHub.Api.DTOs.Auth;
using StudentJobHub.Api.Models;

namespace StudentJobHub.Api.Services;

public class AuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    public async Task<(bool Success, string Message, string? Token, string? RefreshToken)> RegisterAsync(
        RegisterDto dto)
    {
        var allowedRoles = new[] { "Student", "Lecturer", "Business" };

        if (!allowedRoles.Contains(dto.Role, StringComparer.OrdinalIgnoreCase))
        {
            return (false, "Invalid role selected.", null, null);
        }

        var existingUser = await _userManager.FindByEmailAsync(dto.Email);

        if (existingUser != null)
        {
            return (false, "A user with this email already exists.", null, null);
        }

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.FullName,
            PhoneNumber = dto.PhoneNumber,
            University = dto.University
        };

        var result = await _userManager.CreateAsync(user, dto.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(
                "; ",
                result.Errors.Select(error => error.Description));

            return (false, errors, null, null);
        }

        await _userManager.AddToRoleAsync(
            user,
            NormalizeRole(dto.Role));

        var tokens = await IssueTokensAsync(user);

            await _auditLogService.LogAsync(
        user.Id,
        "LOGIN",
        "Authentication",
        user.Id,
        "User logged in successfully.");

        return (true, "Registration successful.", tokens.Token, tokens.RefreshToken);
    }

    public async Task<(bool Success, string Message, string? Token, string? RefreshToken)> LoginAsync(
        LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);

        if (user == null)
        {
            return (false, "Invalid email or password.", null, null);
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return (false, "This account is suspended.", null, null);
        }

        var validPassword = await _userManager.CheckPasswordAsync(
            user,
            dto.Password);

        if (!validPassword)
        {
            return (false, "Invalid email or password.", null, null);
        }

        var tokens = await IssueTokensAsync(user);

        return (true, "Login successful.", tokens.Token, tokens.RefreshToken);
    }

    public async Task<(bool Success, string Message, string? Token, string? RefreshToken)> RefreshAsync(
        string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return (false, "Invalid refresh token.", null, null);
        }

        var tokenHash = HashRefreshToken(refreshToken);
        var user = await _userManager.Users.FirstOrDefaultAsync(
            candidate => candidate.RefreshTokenHash == tokenHash);

        if (user == null || user.RefreshTokenExpiresAt <= DateTime.UtcNow ||
            await _userManager.IsLockedOutAsync(user))
        {
            return (false, "Invalid or expired refresh token.", null, null);
        }

        var tokens = await IssueTokensAsync(user);
        return (true, "Token refreshed.", tokens.Token, tokens.RefreshToken);
    }

    private async Task<(string Token, string RefreshToken)> IssueTokensAsync(
        ApplicationUser user)
    {
        var token = await GenerateJwtTokenAsync(user);
        var refreshToken = Convert.ToBase64String(
            RandomNumberGenerator.GetBytes(64));

        user.RefreshTokenHash = HashRefreshToken(refreshToken);
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(30);

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException("Could not save the refresh token.");
        }

        return (token, refreshToken);
    }

    private static string HashRefreshToken(string refreshToken)
    {
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
    }

    private async Task<string> GenerateJwtTokenAsync(
        ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.MobilePhone, user.PhoneNumber ?? string.Empty)
        };

        claims.AddRange(
            roles.Select(role =>
                new Claim(ClaimTypes.Role, role)));

        var key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "JWT key is not configured.");

        var issuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException(
                "JWT issuer is not configured.");

        var audience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException(
                "JWT audience is not configured.");

        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(key));

        var credentials = new SigningCredentials(
            securityKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string NormalizeRole(string role)
    {
        return role.ToLowerInvariant() switch
        {
            "student" => "Student",
            "lecturer" => "Lecturer",
            "business" => "Business",
            _ => role
        };
    }
}
