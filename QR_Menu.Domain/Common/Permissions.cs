namespace QR_Menu.Domain.Common;

/// <summary>
/// Comprehensive permissions system for fine-grained access control
/// Each permission represents a specific action that can be performed
/// </summary>
public static class Permissions
{
    // User Management Permissions
    public static class Users
    {
        public const string View = "users.view";
        public const string ViewAll = "users.view_all";
        public const string ViewDetails = "users.view_details";
        public const string Create = "users.create";
        public const string Update = "users.update";
        public const string Delete = "users.delete";
        public const string ManageRoles = "users.manage_roles";
        public const string ViewSensitiveData = "users.view_sensitive";
        public const string Export = "users.export";
        public const string BulkOperations = "users.bulk_operations";
    }

    // Restaurant Management Permissions
    public static class Restaurants
    {
        public const string View = "restaurants.view";
        public const string ViewAll = "restaurants.view_all";
        public const string ViewOwn = "restaurants.view_own";
        public const string ViewLicensed = "restaurants.view_licensed";
        public const string Create = "restaurants.create";
        public const string Update = "restaurants.update";
        public const string UpdateOwn = "restaurants.update_own";
        public const string Delete = "restaurants.delete";
        public const string ManageOwnership = "restaurants.manage_ownership";
        public const string ViewAnalytics = "restaurants.view_analytics";
        public const string Export = "restaurants.export";
        public const string BulkOperations = "restaurants.bulk_operations";
    }

    // License Management Permissions
    public static class Licenses
    {
        public const string View = "licenses.view";
        public const string ViewAll = "licenses.view_all";
        public const string ViewOwn = "licenses.view_own";
        public const string Create = "licenses.create";
        public const string Update = "licenses.update";
        public const string Delete = "licenses.delete";
        public const string Extend = "licenses.extend";
        public const string Activate = "licenses.activate";
        public const string Deactivate = "licenses.deactivate";
        public const string ViewFinancials = "licenses.view_financials";
        public const string ManagePricing = "licenses.manage_pricing";
        public const string Export = "licenses.export";
        public const string BulkOperations = "licenses.bulk_operations";
    }

    // Dashboard & Analytics Permissions
    public static class Dashboard
    {
        public const string ViewBasic = "dashboard.view_basic";
        public const string ViewAdvanced = "dashboard.view_advanced";
        public const string ViewFinancials = "dashboard.view_financials";
        public const string ViewUserStats = "dashboard.view_user_stats";
        public const string ViewRestaurantStats = "dashboard.view_restaurant_stats";
        public const string ViewLicenseStats = "dashboard.view_license_stats";
        public const string Export = "dashboard.export";
    }

    // Order Management Permissions
    public static class Orders
    {
        public const string View = "orders.view";
        public const string ViewAll = "orders.view_all";
        public const string ViewOwn = "orders.view_own";
        public const string Create = "orders.create";
        public const string Update = "orders.update";
        public const string Delete = "orders.delete";
        public const string ManageStatus = "orders.manage_status";
        public const string ViewFinancials = "orders.view_financials";
        public const string Export = "orders.export";
    }

    // Category & Product Management
    public static class Menu
    {
        public const string View = "menu.view";
        public const string ViewAll = "menu.view_all";
        public const string ViewOwn = "menu.view_own";
        public const string Create = "menu.create";
        public const string Update = "menu.update";
        public const string Delete = "menu.delete";
        public const string ManagePricing = "menu.manage_pricing";
        public const string Export = "menu.export";
        public const string BulkOperations = "menu.bulk_operations";
    }

    // System Administration
    public static class System
    {
        public const string ViewLogs = "system.view_logs";
        public const string ManageSettings = "system.manage_settings";
        public const string ManagePermissions = "system.manage_permissions";
        public const string ViewSystemStats = "system.view_stats";
        public const string ManageBackup = "system.manage_backup";
        public const string ManageMaintenance = "system.manage_maintenance";
    }

    // Financial Management
    public static class Finance
    {
        public const string ViewRevenue = "finance.view_revenue";
        public const string ViewAllFinancials = "finance.view_all";
        public const string ViewOwnFinancials = "finance.view_own";
        public const string ManagePricing = "finance.manage_pricing";
        public const string ViewReports = "finance.view_reports";
        public const string Export = "finance.export";
    }

    /// <summary>
    /// Get all permissions for a specific module
    /// </summary>
    public static string[] GetModulePermissions(string module)
    {
        return module.ToLower() switch
        {
            "users" => GetAllPermissions(typeof(Users)),
            "restaurants" => GetAllPermissions(typeof(Restaurants)),
            "licenses" => GetAllPermissions(typeof(Licenses)),
            "dashboard" => GetAllPermissions(typeof(Dashboard)),
            "orders" => GetAllPermissions(typeof(Orders)),
            "menu" => GetAllPermissions(typeof(Menu)),
            "system" => GetAllPermissions(typeof(System)),
            "finance" => GetAllPermissions(typeof(Finance)),
            _ => Array.Empty<string>()
        };
    }

    /// <summary>
    /// Get all permissions from a static class using reflection
    /// </summary>
    private static string[] GetAllPermissions(Type type)
    {
        return type.GetFields(global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Static)
                   .Where(f => f.FieldType == typeof(string))
                   .Select(f => (string)f.GetValue(null)!)
                   .ToArray();
    }

    /// <summary>
    /// Get all permissions in the system
    /// </summary>
    public static string[] GetAllPermissions()
    {
        var allPermissions = new List<string>();
        allPermissions.AddRange(GetAllPermissions(typeof(Users)));
        allPermissions.AddRange(GetAllPermissions(typeof(Restaurants)));
        allPermissions.AddRange(GetAllPermissions(typeof(Licenses)));
        allPermissions.AddRange(GetAllPermissions(typeof(Dashboard)));
        allPermissions.AddRange(GetAllPermissions(typeof(Orders)));
        allPermissions.AddRange(GetAllPermissions(typeof(Menu)));
        allPermissions.AddRange(GetAllPermissions(typeof(System)));
        allPermissions.AddRange(GetAllPermissions(typeof(Finance)));
        return allPermissions.ToArray();
    }
} 