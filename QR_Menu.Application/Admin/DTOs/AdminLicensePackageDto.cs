namespace QR_Menu.Application.Admin.DTOs;

public class AdminLicensePackageDto
{
    public Guid Id { get; set; }
    public Guid EntityGuid { get; set; }
    public int LicenseTypeId { get; set; }
    public int Time { get; set; }
    public double UserPrice { get; set; }
    public double DealerPrice { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public DateTime LastUpdateDateTime { get; set; }
    
    // Statistics
    public int LicensesCount { get; set; }
    public int ActiveLicensesCount { get; set; }
    public double TotalRevenue { get; set; }
    public double MonthlyRevenue { get; set; }
} 