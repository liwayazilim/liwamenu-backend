namespace QR_Menu.Domain.Common;


public static class Roles
{
    // Core System Roles
    public const string Manager = "Manager"; // Super admin role
    public const string Owner = "Owner";
    public const string Dealer = "Dealer";

   
    // Get all permissions for a specific role
    //This is where we define what each role can do
   
    public static string[] GetRolePermissions(string role)
    {
        return role switch
        {
            Manager => GetManagerPermissions(), // Manager has all permissions
            Owner => GetOwnerPermissions(),
            Dealer => GetDealerPermissions(),
            _ => Array.Empty<string>()
        };
    }

    /// <summary>
    /// Manager: Full system access - Super Admin role
    /// </summary>
    private static string[] GetManagerPermissions()
    {
        return Permissions.GetAllPermissions(); // All permissions - Manager is Super Admin
    }

    /// <summary>
    /// Owner: Can manage their own restaurants and view their data
    /// </summary>
    private static string[] GetOwnerPermissions()
    {
        return new[]
        {
            // Own Restaurant Management
            Permissions.Restaurants.ViewOwn,
            Permissions.Restaurants.UpdateOwn,

            // Own Menu Management
            Permissions.Menu.ViewOwn,
            Permissions.Menu.Create,
            Permissions.Menu.Update,
            Permissions.Menu.Delete,
            Permissions.Menu.ManagePricing,

            // Own Order Management
            Permissions.Orders.ViewOwn,
            Permissions.Orders.Update,
            Permissions.Orders.ManageStatus,
            Permissions.Orders.Create, // Owners can create orders for their restaurants

            // Own Financial Data
            Permissions.Finance.ViewOwnFinancials,

            // Own License Viewing
            Permissions.Licenses.ViewOwn
        };
    }

    /// <summary>
    /// Dealer: Can manage licensed restaurants and users under them
    /// </summary>
    private static string[] GetDealerPermissions()
    {
        return new[]
        {
            // User Management (limited)
            Permissions.Users.View,
            Permissions.Users.Create,
            Permissions.Users.Update,

            // Licensed Restaurant Management
            Permissions.Restaurants.ViewLicensed,
            Permissions.Restaurants.Update,
            Permissions.Restaurants.ViewAnalytics,

            // License Management (own licenses)
            Permissions.Licenses.ViewOwn,
            Permissions.Licenses.Create,
            Permissions.Licenses.Update,
            Permissions.Licenses.Extend,
            Permissions.Licenses.Activate,
            Permissions.Licenses.Deactivate,

            // Order Management (licensed restaurants)
            Permissions.Orders.View,
            Permissions.Orders.ViewFinancials,
            Permissions.Orders.Create,

            // Menu Management (licensed restaurants)
            Permissions.Menu.View,
            Permissions.Menu.Update,

            // Financial Access (own data)
            Permissions.Finance.ViewOwnFinancials,
            Permissions.Finance.ViewReports
        };
    }

    /// <summary>
    /// Get all defined roles in the system
    /// </summary>
    public static string[] GetAllRoles()
    {
        return new[]
        {
            Manager,
            Owner,
            Dealer
        };
    }

    /// <summary>
    /// Check if a role has a specific permission
    /// </summary>
    public static bool HasPermission(string role, string permission)
    {
        var rolePermissions = GetRolePermissions(role);
        return rolePermissions.Contains(permission);
    }

    /// <summary>
    /// Get role hierarchy level (lower number = higher privilege)
    /// </summary>
    public static int GetRoleLevel(string role)
    {
        return role switch
        {
            Manager => 0, // Manager is Super Admin
            Dealer => 1,
            Owner => 2,
            _ => 999
        };
    }
} 