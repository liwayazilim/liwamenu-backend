using Microsoft.AspNetCore.Authorization;

namespace QR_Menu.Infrastructure.Authorization;

/// <summary>
/// Custom authorization requirement for permission-based access control
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
} 