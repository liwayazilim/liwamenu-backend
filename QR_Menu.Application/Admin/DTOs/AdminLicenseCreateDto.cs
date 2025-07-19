namespace QR_Menu.Application.Admin.DTOs;

public class AdminLicenseCreateDto
{
    public Guid UserId { get; set; }
    public Guid? RestaurantId { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public bool IsActive { get; set; } = true;
    public double? UserPrice { get; set; }
    public double? DealerPrice { get; set; }
} 