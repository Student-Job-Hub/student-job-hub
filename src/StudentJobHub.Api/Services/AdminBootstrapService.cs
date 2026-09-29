using Microsoft.AspNetCore.Identity;
using StudentJobHub.Api.Models;

namespace StudentJobHub.Api.Services;

public class AdminBootstrapService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminBootstrapService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task EnsureAdminAsync(string? email, string? password)
    {
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Both AdminBootstrap:Email and AdminBootstrap:Password must be configured.");
        }

        email = email.Trim();
        if (!await _roleManager.RoleExistsAsync("Admin"))
        {
            throw new InvalidOperationException(
                "The Admin role must exist before the bootstrap account can be provisioned.");
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = "Platform Administrator"
            };

            var createResult = await _userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Could not create the configured admin account: " +
                    string.Join("; ", createResult.Errors.Select(error => error.Description)));
            }
        }

        if (!await _userManager.IsInRoleAsync(user, "Admin"))
        {
            var roleResult = await _userManager.AddToRoleAsync(user, "Admin");
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Could not assign the Admin role: " +
                    string.Join("; ", roleResult.Errors.Select(error => error.Description)));
            }
        }
    }
}
