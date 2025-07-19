namespace QR_Menu.Application.Admin.DTOs;

public class AdminLicenseRevenueDto
{
    public string Month { get; set; } = string.Empty;
    public decimal UserRevenue { get; set; }
    public decimal DealerRevenue { get; set; }
    public decimal TotalRevenue { get; set; }
    public int LicensesCount { get; set; }
} 