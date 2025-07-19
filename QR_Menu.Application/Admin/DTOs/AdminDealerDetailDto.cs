namespace QR_Menu.Application.Admin.DTOs;

public class AdminDealerDetailDto : AdminUserDto
{
    public List<AdminLicenseDto> Licenses { get; set; } = new();
    public List<AdminRestaurantSummaryDto> ManagedRestaurants { get; set; } = new();
    public DealerStatsDto Stats { get; set; } = new();
}

public class DealerStatsDto
{
    public int TotalLicenses { get; set; }
    public int ActiveLicenses { get; set; }
    public int ExpiredLicenses { get; set; }
    public int ManagedRestaurants { get; set; }
    public int ActiveRestaurants { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public int TotalCustomers { get; set; }
    public DateTime? LastActivity { get; set; }
} 