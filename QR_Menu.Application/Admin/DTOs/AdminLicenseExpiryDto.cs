namespace QR_Menu.Application.Admin.DTOs;

public class AdminLicenseExpiryDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? RestaurantName { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int DaysRemaining { get; set; }
    public bool IsExpired { get; set; }
} 