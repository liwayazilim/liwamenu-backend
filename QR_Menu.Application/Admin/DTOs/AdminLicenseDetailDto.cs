namespace QR_Menu.Application.Admin.DTOs;

public class AdminLicenseDetailDto : AdminLicenseDto
{
    public AdminUserSummaryDto User { get; set; } = new();
    public AdminRestaurantSummaryDto? Restaurant { get; set; }
    public List<AdminLicenseHistoryDto> History { get; set; } = new();
} 