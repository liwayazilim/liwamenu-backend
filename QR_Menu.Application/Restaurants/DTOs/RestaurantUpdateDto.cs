using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace QR_Menu.Application.Restaurants.DTOs;

public class RestaurantUpdateDto
{
    [Required(ErrorMessage = "Restoran ID zorunludur")]
    public Guid RestaurantId { get; set; }
    
    public string? Name { get; set; }
    
    public string? PhoneNumber { get; set; }
    
    public string? City { get; set; }
    
    public string? District { get; set; }
    
    public string? Neighbourhood { get; set; }
    
    public string? Address { get; set; }
    
    public double? Latitude { get; set; }
    
    public double? Longitude { get; set; }
    
    // Image properties (for form-data) - optional
    public IFormFile? Image { get; set; }
    
    public int? ThemeId { get; set; }
} 