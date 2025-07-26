namespace QR_Menu.Application.Admin.DTOs;

public class AdminLicenseDto
{
    public Guid Id { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public bool IsActive { get; set; }
    public bool IsExpired => DateTime.UtcNow > EndDateTime;
    public int DaysRemaining => IsExpired ? 0 : (int)(EndDateTime - DateTime.UtcNow).TotalDays;
    public double? UserPrice { get; set; }
    public double? DealerPrice { get; set; }
    
    // User (Dealer) Information
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string? UserPhone { get; set; }
    public bool UserIsActive { get; set; }
    public bool UserIsDealer { get; set; }
    
    // Restaurant Information
    public Guid? RestaurantId { get; set; }
    public string? RestaurantName { get; set; }
    public string? RestaurantCity { get; set; }
    public string? RestaurantDistrict { get; set; }
    public bool? RestaurantIsActive { get; set; }
    public string? RestaurantOwnerName { get; set; }
    public string? RestaurantOwnerEmail { get; set; }
    
    // Status Information
    public string Status => IsExpired ? "Expired" : IsActive ? "Active" : "Inactive";
    public string LicenseType => RestaurantId.HasValue ? "Restaurant License" : "General License";
    public DateTime CreatedDateTime { get; set; }
    public DateTime LastUpdateDateTime { get; set; }
} 