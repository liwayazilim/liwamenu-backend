namespace QR_Menu.Application.Admin.DTOs;

public class AdminOwnerDetailDto : AdminUserDto
{
    public List<AdminRestaurantSummaryDto> Restaurants { get; set; } = new();
    public List<AdminLicenseSummaryDto> Licenses { get; set; } = new();
    public OwnerStatsDto Stats { get; set; } = new();
    public AdminUserSummaryDto? AssignedDealer { get; set; }
}

public class OwnerStatsDto
{
    public int TotalRestaurants { get; set; }
    public int ActiveRestaurants { get; set; }
    public int TotalLicenses { get; set; }
    public int ActiveLicenses { get; set; }
    public int TotalProducts { get; set; }
    public int TotalOrders { get; set; }
    public int MonthlyOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public DateTime? LastActivity { get; set; }
    public bool HasDealer { get; set; }
} 