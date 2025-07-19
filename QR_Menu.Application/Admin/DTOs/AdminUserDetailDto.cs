namespace QR_Menu.Application.Admin.DTOs;

public class AdminUserDetailDto : AdminUserDto
{
    public List<AdminRestaurantSummaryDto> Restaurants { get; set; } = new();
    public List<AdminLicenseSummaryDto> Licenses { get; set; } = new();
} 