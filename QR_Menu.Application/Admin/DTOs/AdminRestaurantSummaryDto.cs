namespace QR_Menu.Application.Admin.DTOs;

public class AdminRestaurantSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool HasLicense { get; set; }
    public DateTime? LicenseExpiry { get; set; }
    public int CategoriesCount { get; set; }
    public int ProductsCount { get; set; }
    public int OrdersCount { get; set; }
} 