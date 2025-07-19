namespace QR_Menu.Application.Admin.DTOs;

public class AdminLicenseUpdateDto
{
    public DateTime? StartDateTime { get; set; }
    public DateTime? EndDateTime { get; set; }
    public bool? IsActive { get; set; }
    public double? UserPrice { get; set; }
    public double? DealerPrice { get; set; }
    public Guid? RestaurantId { get; set; }
} 