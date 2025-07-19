namespace QR_Menu.Application.Admin.DTOs;

public class AdminLicenseStatsDto
{
    public int TotalLicenses { get; set; }
    public int ActiveLicenses { get; set; }
    public int ExpiredLicenses { get; set; }
    public int ExpiringThisMonth { get; set; }
    public int ExpiringThisWeek { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public List<AdminLicenseRevenueDto> MonthlyRevenueBreakdown { get; set; } = new();
    public List<AdminLicenseExpiryDto> ExpiringLicenses { get; set; } = new();
} 