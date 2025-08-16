using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace QR_Menu.Application.Restaurants.DTOs;

public class RestaurantCreateDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public string PhoneNumber { get; set; } = string.Empty;
    
    [Required]
    public double Latitude { get; set; }
    
    [Required]
    public double Longitude { get; set; }
    
    [Required]
    public string City { get; set; } = string.Empty;
    
    [Required]
    public string District { get; set; } = string.Empty;
    
    public string? Neighbourhood { get; set; }
    
    [Required]
    public string Address { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
    
    public int? ThemeId { get; set; }
    
    // Image properties (for form-data)
    public IFormFile? Image { get; set; }
} 