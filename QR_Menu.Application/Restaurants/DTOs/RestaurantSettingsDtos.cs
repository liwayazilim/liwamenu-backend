using System.ComponentModel.DataAnnotations;

namespace QR_Menu.Application.Restaurants.DTOs;

public class RestaurantSettingsUpdateDto
{
    [Required(ErrorMessage = "Restoran ID zorunludur")]
    public Guid RestaurantId { get; set; }

    [Range(0, 100, ErrorMessage = "Minimum mesafe 0-100 km arasında olmalıdır")]
    public double? MinDistance { get; set; }

    [StringLength(100, ErrorMessage = "Google Analytics ID 100 karakteri geçemez")]
    public string? GoogleAnalytics { get; set; }

    [StringLength(20, ErrorMessage = "Varsayılan dil 20 karakteri geçemez")]
    public string? DefaultLang { get; set; }

    public bool? InPersonOrder { get; set; }

    public bool? OnlineOrder { get; set; }

    [StringLength(200, ErrorMessage = "Slogan 1 200 karakteri geçemez")]
    public string? Slogan1 { get; set; }

    [StringLength(200, ErrorMessage = "Slogan 2 200 karakteri geçemez")]
    public string? Slogan2 { get; set; }

    public bool? Hide { get; set; }
}

public class RestaurantSettingsResponseDto
{
    public Guid RestaurantId { get; set; }
    public double? MinDistance { get; set; }
    public string? GoogleAnalytics { get; set; }
    public string? DefaultLang { get; set; }
    public bool InPersonOrder { get; set; }
    public bool OnlineOrder { get; set; }
    public string? Slogan1 { get; set; }
    public string? Slogan2 { get; set; }
    public bool Hide { get; set; }
    public DateTime LastUpdateDateTime { get; set; }
} 