namespace QR_Menu.Domain.Common;


public static class Roles
{
    // Core System Roles
    public const string SuperAdmin = "SuperAdmin";
    public const string Manager = "Manager";
    public const string Owner = "Owner";
    public const string Dealer = "Dealer";
    public const string Customer = "Customer";

   
    // Get all permissions for a specific role
    //This is where we define what each role can do
   
    public static string[] GetRolePermissions(string role)
    {
        return role switch
        {
            SuperAdmin => GetSuperAdminPermissions(),
            Manager => GetManagerPermissions(),
            Owner => GetOwnerPermissions(),
            Dealer => GetDealerPermissions(),
            Customer => GetCustomerPermissions(),
            _ => Array.Empty<string>()
        };
    }

    /// <summary>
    /// Super Admin: Full system access - should be very limited
    /// </summary>
    private static string[] GetSuperAdminPermissions()
    {
        return Permissions.GetAllPermissions(); // All permissions
    }

    /// <summary>
    /// Manager: Can manage users, restaurants, and licenses but no system admin
    /// </summary>
    private static string[] GetManagerPermissions()
    {
        return new[]
        {
            // User Management
            Permissions.Users.View,
            Permissions.Users.ViewAll,
            Permissions.Users.ViewDetails,
            Permissions.Users.Create,
            Permissions.Users.Update,
            Permissions.Users.Delete,
            Permissions.Users.Export,
            Permissions.Users.BulkOperations,

            // Restaurant Management
            Permissions.Restaurants.View,
            Permissions.Restaurants.ViewAll,
            Permissions.Restaurants.Create,
            Permissions.Restaurants.Update,
            Permissions.Restaurants.Delete,
            Permissions.Restaurants.ManageOwnership,
            Permissions.Restaurants.ViewAnalytics,
            Permissions.Restaurants.Export,
            Permissions.Restaurants.BulkOperations,

            // License Management
            Permissions.Licenses.View,
            Permissions.Licenses.ViewAll,
            Permissions.Licenses.Create,
            Permissions.Licenses.Update,
            Permissions.Licenses.Delete,
            Permissions.Licenses.Extend,
            Permissions.Licenses.Activate,
            Permissions.Licenses.Deactivate,
            Permissions.Licenses.ViewFinancials,
            Permissions.Licenses.ManagePricing,
            Permissions.Licenses.Export,
            Permissions.Licenses.BulkOperations,

            // Dashboard Access
            Permissions.Dashboard.ViewBasic,
            Permissions.Dashboard.ViewAdvanced,
            Permissions.Dashboard.ViewFinancials,
            Permissions.Dashboard.ViewUserStats,
            Permissions.Dashboard.ViewRestaurantStats,
            Permissions.Dashboard.ViewLicenseStats,
            Permissions.Dashboard.Export,

            // Order Management
            Permissions.Orders.View,
            Permissions.Orders.ViewAll,
            Permissions.Orders.Update,
            Permissions.Orders.ManageStatus,
            Permissions.Orders.ViewFinancials,
            Permissions.Orders.Export,

            // Menu Management
            Permissions.Menu.View,
            Permissions.Menu.ViewAll,
            Permissions.Menu.Create,
            Permissions.Menu.Update,
            Permissions.Menu.Delete,
            Permissions.Menu.ManagePricing,
            Permissions.Menu.Export,
            Permissions.Menu.BulkOperations,

            // Financial Access
            Permissions.Finance.ViewRevenue,
            Permissions.Finance.ViewAllFinancials,
            Permissions.Finance.ManagePricing,
            Permissions.Finance.ViewReports,
            Permissions.Finance.Export
        };
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

            // Own Financial Data
            Permissions.Finance.ViewOwnFinancials,

            // Basic Dashboard
            Permissions.Dashboard.ViewBasic,

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

            // Dashboard Access (limited)
            Permissions.Dashboard.ViewBasic,
            Permissions.Dashboard.ViewRestaurantStats,
            Permissions.Dashboard.ViewLicenseStats,

            // Order Management (licensed restaurants)
            Permissions.Orders.View,
            Permissions.Orders.ViewFinancials,

            // Menu Management (licensed restaurants)
            Permissions.Menu.View,
            Permissions.Menu.Update,

            // Financial Access (own data)
            Permissions.Finance.ViewOwnFinancials,
            Permissions.Finance.ViewReports
        };
    }

    /// <summary>
    /// Customer: Can only place orders and view public data
    /// </summary>
    private static string[] GetCustomerPermissions()
    {
        return new[]
        {
            // Basic Restaurant Viewing
            Permissions.Restaurants.View,

            // Menu Viewing
            Permissions.Menu.View,

            // Order Management (own orders)
            Permissions.Orders.Create,
            Permissions.Orders.ViewOwn
        };
    }

    /// <summary>
    /// Get all defined roles in the system
    /// </summary>
    public static string[] GetAllRoles()
    {
        return new[]
        {
            SuperAdmin,
            Manager,
            Owner,
            Dealer,
            Customer
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
            SuperAdmin => 0,
            Manager => 1,
            Dealer => 2,
            Owner => 3,
            Customer => 4,
            _ => 999
        };
    }
} 