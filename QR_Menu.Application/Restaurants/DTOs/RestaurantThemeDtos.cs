using System.ComponentModel.DataAnnotations;

namespace QR_Menu.Application.Restaurants.DTOs;

public class RestaurantThemeUpdateDto
{
    [Required(ErrorMessage = "Restoran ID zorunludur")]
    public Guid RestaurantId { get; set; }

    [Range(0, 14, ErrorMessage = "Tema ID 0-14 aralığında olmalıdır")]
    public int ThemeId { get; set; }
}

public class RestaurantThemeResponseDto
{
    public Guid RestaurantId { get; set; }
    public int ThemeId { get; set; }
    public DateTime LastUpdateDateTime { get; set; }
} 