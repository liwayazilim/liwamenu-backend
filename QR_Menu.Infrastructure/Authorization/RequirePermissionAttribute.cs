using Microsoft.AspNetCore.Authorization;

namespace QR_Menu.Infrastructure.Authorization;

/// <summary>
/// Custom authorization attribute for permission-based access control
/// Usage: [RequirePermission(Permissions.Users.View)]
/// </summary>
public class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permission) : base()
    {
        Policy = $"Permission.{permission}";
    }
} 