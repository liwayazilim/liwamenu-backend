using Microsoft.AspNetCore.Http;

namespace QR_Menu.Application.Restaurants.DTOs;

public class RestaurantUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string? Neighbourhood { get; set; }
    public string Address { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lng { get; set; }
    public bool IsActive { get; set; } = true;
    public string? WorkingHours { get; set; }
    public double? MinDistance { get; set; }
    public string? GoogleAnalytics { get; set; }
    public string? DefaultLang { get; set; }
    public bool InPersonOrder { get; set; } = true;
    public bool OnlineOrder { get; set; } = true;
    public string? Slogan1 { get; set; }
    public string? Slogan2 { get; set; }
    public bool Hide { get; set; } = false;
    
    // Image properties (for form-data)
    public IFormFile? Image { get; set; }
} 