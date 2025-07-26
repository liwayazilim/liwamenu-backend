using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QR_Menu.Domain;
using QR_Menu.Domain.Common;

namespace QR_Menu.Infrastructure.Seeding;

public class DatabaseSeeder
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(IServiceProvider serviceProvider, ILogger<DatabaseSeeder> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }


    public async Task SeedAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        await SeedRolesAsync(roleManager);
        await SeedInitialUsersAsync(userManager);
    }

   
    private async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roleManager)
    {
        _logger.LogInformation("Seeding system roles...");

        var roles = Roles.GetAllRoles();
        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var role = new IdentityRole<Guid>(roleName);
                var result = await roleManager.CreateAsync(role);
                
                if (result.Succeeded)
                {
                    _logger.LogInformation($"Created role: {roleName}");
                }
                else
                {
                    _logger.LogError($"Failed to create role {roleName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }
    }


    private async Task SeedInitialUsersAsync(UserManager<User> userManager)
    {
        _logger.LogInformation("Seeding initial users...");

        // Seed Manager (Super Admin)
        await SeedUserAsync(userManager, new User
        {
            Id = Guid.NewGuid(),
            UserName = "abouadmin@qrmenu.com",
            Email = "abouadmin@qrmenu.com",
            FirstName = "Abou",
            LastName = "Admin",
            Role = UserRole.Manager, // Updated to Manager
            IsActive = true,
            EmailConfirmed = true,
            IsDealer = false,
            CreatedDateTime = DateTime.UtcNow,
            LastUpdateDateTime = DateTime.UtcNow
        }, "abou1234", Roles.Manager);

        // Seed Demo Owner
        await SeedUserAsync(userManager, new User
        {
            Id = Guid.NewGuid(),
            UserName = "owner@qrmenu.com",
            Email = "owner@qrmenu.com",
            FirstName = "Demo",
            LastName = "Owner",
            Role = UserRole.Owner,
            IsActive = true,
            EmailConfirmed = true,
            IsDealer = false,
            City = "Demo City",
            District = "Demo District",
            CreatedDateTime = DateTime.UtcNow,
            LastUpdateDateTime = DateTime.UtcNow
        }, "Owner123!", Roles.Owner);

        // Seed Demo Dealer
        await SeedUserAsync(userManager, new User
        {
            Id = Guid.NewGuid(),
            UserName = "dealer@qrmenu.com",
            Email = "dealer@qrmenu.com",
            FirstName = "Demo",
            LastName = "Dealer",
            Role = UserRole.Dealer,
            IsActive = true,
            EmailConfirmed = true,
            IsDealer = true,
            City = "Demo City",
            District = "Demo District",
            CreatedDateTime = DateTime.UtcNow,
            LastUpdateDateTime = DateTime.UtcNow
        }, "Dealer123!", Roles.Dealer);
    }

    
    private async Task SeedUserAsync(UserManager<User> userManager, User user, string password, string roleName)
    {
        var existingUser = await userManager.FindByEmailAsync(user.Email);
        if (existingUser == null)
        {
            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, roleName);
                _logger.LogInformation($"Created user: {user.Email} with role: {roleName}");
            }
            else
            {
                _logger.LogError($"Failed to create user {user.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
        else
        {
            // Ensure existing user has correct role
            var userRoles = await userManager.GetRolesAsync(existingUser);
            if (!userRoles.Contains(roleName))
            {
                await userManager.AddToRoleAsync(existingUser, roleName);
                _logger.LogInformation($"Added role {roleName} to existing user: {existingUser.Email}");
            }
        }
    }
} 