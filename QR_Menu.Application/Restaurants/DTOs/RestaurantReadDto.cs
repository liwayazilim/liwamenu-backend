namespace QR_Menu.Application.Restaurants.DTOs;

public class RestaurantReadDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? DealerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string? Neighbourhood { get; set; }
    public string Address { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lng { get; set; }
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
    
    // Image properties
    public string? ImageFileName { get; set; }
    public string? ImageContentType { get; set; }
    public bool HasImage => !string.IsNullOrEmpty(ImageFileName);
    public string? ImageUrl => HasImage ? $"/images/restaurants/{ImageFileName}" : null;
} 