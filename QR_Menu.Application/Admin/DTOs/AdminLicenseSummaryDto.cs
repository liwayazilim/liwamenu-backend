namespace QR_Menu.Application.Admin.DTOs;

public class AdminLicenseSummaryDto
{
    public Guid Id { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public bool IsActive { get; set; }
    public bool IsExpired => DateTime.UtcNow > EndDateTime;
    public double? UserPrice { get; set; }
    public double? DealerPrice { get; set; }
    public string? RestaurantName { get; set; }
    public Guid? RestaurantId { get; set; }
} 