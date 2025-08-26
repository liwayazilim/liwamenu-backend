using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await SeedRolesAsync(roleManager);
        await SeedInitialUsersAsync(userManager);
        await SeedPaymentMethodsAsync(context);
        await SeedOrderTagsAsync(context);
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
        if (string.IsNullOrEmpty(user.Email))
        {
            _logger.LogWarning($"User {user.UserName} has no email address, skipping user creation");
            return;
        }
        
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

    private async Task SeedPaymentMethodsAsync(AppDbContext context)
    {
        _logger.LogInformation("Seeding payment methods...");

        var defaultPaymentMethods = new[]
        {
            new PaymentMethod
            {
                Id = Guid.NewGuid(),
                Name = "Nakit",
                IsActive = true,
                CreatedDateTime = DateTime.UtcNow,
                LastUpdateDateTime = DateTime.UtcNow
            },
            new PaymentMethod
            {
                Id = Guid.NewGuid(),
                Name = "Kredi Kartı",
                IsActive = true,
                CreatedDateTime = DateTime.UtcNow,
                LastUpdateDateTime = DateTime.UtcNow
            },
            new PaymentMethod
            {
                Id = Guid.NewGuid(),
                Name = "Banka Kartı",
                IsActive = true,
                CreatedDateTime = DateTime.UtcNow,
                LastUpdateDateTime = DateTime.UtcNow
            },
            new PaymentMethod
            {
                Id = Guid.NewGuid(),
                Name = "Havale/EFT",
                IsActive = true,
                CreatedDateTime = DateTime.UtcNow,
                LastUpdateDateTime = DateTime.UtcNow
            },
            new PaymentMethod
            {
                Id = Guid.NewGuid(),
                Name = "Metropol",
                IsActive = true,
                CreatedDateTime = DateTime.UtcNow,
                LastUpdateDateTime = DateTime.UtcNow
            },
            new PaymentMethod
            {
                Id = Guid.NewGuid(),
                Name = "Multinet",
                IsActive = true,
                CreatedDateTime = DateTime.UtcNow,
                LastUpdateDateTime = DateTime.UtcNow
            },
            new PaymentMethod
            {
                Id = Guid.NewGuid(),
                Name = "Sodexo",
                IsActive = true,
                CreatedDateTime = DateTime.UtcNow,
                LastUpdateDateTime = DateTime.UtcNow
            }
        };

        foreach (var paymentMethod in defaultPaymentMethods)
        {
            var existingMethod = await context.PaymentMethods
                .FirstOrDefaultAsync(pm => pm.Name.ToLower() == paymentMethod.Name.ToLower());
            
            if (existingMethod == null)
            {
                context.PaymentMethods.Add(paymentMethod);
                _logger.LogInformation($"Created payment method: {paymentMethod.Name}");
            }
        }

        await context.SaveChangesAsync();
        _logger.LogInformation("Payment methods seeding completed.");
    }

    private async Task SeedOrderTagsAsync(AppDbContext context)
    {
        _logger.LogInformation("Seeding order tags...");

        // Get the first restaurant to associate tags with
        var firstRestaurant = await context.Restaurants.FirstOrDefaultAsync();
        if (firstRestaurant == null)
        {
            _logger.LogWarning("No restaurants found. Skipping order tags seeding.");
            return;
        }

        var defaultOrderTags = new[]
        {
            // Order-level tags
            new OrderTag
            {
                Id = Guid.NewGuid(),
                RestaurantId = firstRestaurant.Id,
                Name = "Takeaway",
                Description = "Order for takeaway",
                TagType = TagType.OrderLevel,
                IsActive = true,
                DisplayOrder = 1,
                Color = "#28a745",
                Icon = "📦",
                CreatedDateTime = DateTime.UtcNow,
                LastUpdateDateTime = DateTime.UtcNow
            },
            new OrderTag
            {
                Id = Guid.NewGuid(),
                RestaurantId = firstRestaurant.Id,
                Name = "in-resto",
                Description = "Order for dining in restaurant",
                TagType = TagType.OrderLevel,
                IsActive = true,
                DisplayOrder = 2,
                Color = "#007bff",
                Icon = "🍽️",
                CreatedDateTime = DateTime.UtcNow,
                LastUpdateDateTime = DateTime.UtcNow
            },
            
            // Item-level tags
            new OrderTag
            {
                Id = Guid.NewGuid(),
                RestaurantId = firstRestaurant.Id,
                Name = "No Onions",
                Description = "Remove onions from the item",
                TagType = TagType.ItemLevel,
                IsActive = true,
                DisplayOrder = 4,
                Color = "#dc3545",
                Icon = "🚫🧅",
                CreatedDateTime = DateTime.UtcNow,
                LastUpdateDateTime = DateTime.UtcNow
            },
            new OrderTag
            {
                Id = Guid.NewGuid(),
                RestaurantId = firstRestaurant.Id,
                Name = "Extra Cheese",
                Description = "Add extra cheese to the item",
                TagType = TagType.ItemLevel,
                IsActive = true,
                DisplayOrder = 5,
                Color = "#fd7e14",
                Icon = "🧀",
                CreatedDateTime = DateTime.UtcNow,
                LastUpdateDateTime = DateTime.UtcNow
            },
            new OrderTag
            {
                Id = Guid.NewGuid(),
                RestaurantId = firstRestaurant.Id,
                Name = "Spicy",
                Description = "Make the item spicy",
                TagType = TagType.ItemLevel,
                IsActive = true,
                DisplayOrder = 6,
                Color = "#e83e8c",
                Icon = "🌶️",
                CreatedDateTime = DateTime.UtcNow,
                LastUpdateDateTime = DateTime.UtcNow
            },
            new OrderTag
            {
                Id = Guid.NewGuid(),
                RestaurantId = firstRestaurant.Id,
                Name = "Vegetarian",
                Description = "Vegetarian option",
                TagType = TagType.ItemLevel,
                IsActive = true,
                DisplayOrder = 7,
                Color = "#20c997",
                Icon = "🥬",
                CreatedDateTime = DateTime.UtcNow,
                LastUpdateDateTime = DateTime.UtcNow
            }
        };

        foreach (var orderTag in defaultOrderTags)
        {
            var existingTag = await context.OrderTags
                .FirstOrDefaultAsync(ot => ot.RestaurantId == orderTag.RestaurantId && 
                                         ot.Name.ToLower() == orderTag.Name.ToLower());
            
            if (existingTag == null)
            {
                context.OrderTags.Add(orderTag);
                _logger.LogInformation($"Created order tag: {orderTag.Name} ({orderTag.TagType})");
            }
        }

        await context.SaveChangesAsync();
        _logger.LogInformation("Order tags seeding completed.");
    }
} 