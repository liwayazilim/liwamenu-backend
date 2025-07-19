namespace QR_Menu.Application.Admin.DTOs;

public class AdminDashboardStatsDto
{
    // User Statistics
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int VerifiedUsers { get; set; }
    public int NewUsersThisMonth { get; set; }

    // Restaurant Statistics
    public int TotalRestaurants { get; set; }
    public int ActiveRestaurants { get; set; }
    public int RestaurantsWithLicense { get; set; }
    public int NewRestaurantsThisMonth { get; set; }

    // License Statistics
    public int TotalLicenses { get; set; }
    public int ActiveLicenses { get; set; }
    public int ExpiredLicenses { get; set; }
    public int ExpiringThisWeek { get; set; }

    // Order Statistics
    public int TotalOrders { get; set; }
    public int OrdersThisMonth { get; set; }
    public int PendingOrders { get; set; }
    public int CompletedOrders { get; set; }
} 