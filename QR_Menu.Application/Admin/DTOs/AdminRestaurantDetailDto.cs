namespace QR_Menu.Application.Admin.DTOs;

public class AdminRestaurantDetailDto : AdminRestaurantDto
{
    public List<AdminCategoryDto> Categories { get; set; } = new();
    public List<AdminOrderSummaryDto> RecentOrders { get; set; } = new();
    public AdminLicenseDto? License { get; set; }
} 