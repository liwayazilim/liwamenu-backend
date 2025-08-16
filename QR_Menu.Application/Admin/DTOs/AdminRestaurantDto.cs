namespace QR_Menu.Application.Admin.DTOs;

public class AdminRestaurantDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string? Neighbourhood { get; set; }
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsActive { get; set; }
    public string? WorkingHours { get; set; }
    public double? MinDistance { get; set; }
    public string? GoogleAnalytics { get; set; }
    public string? DefaultLang { get; set; }
    public bool InPersonOrder { get; set; }
    public bool OnlineOrder { get; set; }
    public string? Slogan1 { get; set; }
    public string? Slogan2 { get; set; }
    public bool Hide { get; set; }
    public int ThemeId { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public DateTime LastUpdateDateTime { get; set; }
    
    // Owner Information
    public Guid UserId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public string? OwnerPhone { get; set; }
    public string OwnerRole { get; set; } = string.Empty;
    public bool OwnerIsActive { get; set; }
    
    // Dealer Information
    public Guid? DealerId { get; set; }
    public string? DealerName { get; set; }
    public string? DealerEmail { get; set; }
    
    // License Information
    public Guid? LicenseId { get; set; }
    public bool HasLicense { get; set; }
    public DateTime? LicenseStart { get; set; }
    public DateTime? LicenseEnd { get; set; }
    public bool LicenseIsActive { get; set; }
    public bool LicenseIsExpired { get; set; }
    public double? LicenseUserPrice { get; set; }
    public double? LicenseDealerPrice { get; set; }

    // images infos: 
    public string? ImageFileName { get; set; }
    public string? ImageContentType { get; set; }
    public bool HasImage => !string.IsNullOrEmpty(ImageFileName);
    public string? ImageUrl => HasImage ? "/images/restaurants/" + ImageFileName : null;
    public string? ImageAbsoluteUrl { get; set; }
    
    // Statistics
    public int CategoriesCount { get; set; }
    public int ProductsCount { get; set; }
    public int ActiveProductsCount { get; set; }
    public int OrdersCount { get; set; }
    public int PendingOrdersCount { get; set; }
    public int CompletedOrdersCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public DateTime? LastOrderDate { get; set; }
} 