using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using QR_Menu.Domain;
using QR_Menu.Domain.Common;
using System.Security.Claims;

namespace QR_Menu.Infrastructure.Authorization;

/// <summary>
/// Handles permission-based authorization by checking user roles and permissions
/// </summary>
public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly UserManager<User> _userManager;

    public PermissionHandler(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // If user is not authenticated, don't fail here. Let the middleware issue a 401 Challenge.
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        // Get user ID from claims
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier) ?? 
                         context.User.FindFirst("sub");
        
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return;
        }

        // Get user from database
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null || !user.IsActive)
        {
            return;
        }

        // Get user roles
        var userRoles = await _userManager.GetRolesAsync(user);
        
        // Check if any of the user's roles has the required permission
        foreach (var role in userRoles)
        {
            if (Roles.HasPermission(role, requirement.Permission))
            {
                context.Succeed(requirement);
                return;
            }
        }

        // Also check the legacy Role property for backward compatibility
        var legacyRole = user.Role.ToString();
        if (Roles.HasPermission(legacyRole, requirement.Permission))
        {
            context.Succeed(requirement);
            return;
        }

        // Authenticated but lacks permission -> explicit forbid
        context.Fail();
    }
} 