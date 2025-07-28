namespace QR_Menu.Application.Admin.DTOs;

public class AdminRestaurantUpdateDto
{
    public string? Name { get; set; }
    public string? PhoneNumber { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? Neighbourhood { get; set; }
    public string? Address { get; set; }
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public bool? IsActive { get; set; }
    public string? WorkingHours { get; set; }
    public double? MinDistance { get; set; }
    public string? GoogleAnalytics { get; set; }
    public string? DefaultLang { get; set; }
    public bool? InPersonOrder { get; set; }
    public bool? OnlineOrder { get; set; }
    public string? Slogan1 { get; set; }
    public string? Slogan2 { get; set; }
    public bool? Hide { get; set; }
    public Guid? UserId { get; set; } // Allow admin to change owner
    public Guid? DealerId { get; set; } // Allow admin to change dealer
} 