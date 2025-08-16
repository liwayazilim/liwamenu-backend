namespace QR_Menu.Application.Restaurants.DTOs;

using System.ComponentModel.DataAnnotations;

public class SocialLinksReadDto
{
    public Guid RestaurantId { get; set; }
    public string? FacebookUrl { get; set; }
    public string? InstagramUrl { get; set; }
    public string? TiktokUrl { get; set; }
    public string? YoutubeUrl { get; set; }
    public string? WhatsappUrl { get; set; }
}

public class SocialLinksUpdateDto
{
    [Required]
    public Guid RestaurantId { get; set; }

    [Url]
    public string? FacebookUrl { get; set; }

    [Url]
    public string? InstagramUrl { get; set; }

    [Url]
    public string? TiktokUrl { get; set; }

    [Url]
    public string? YoutubeUrl { get; set; }

    [Url]
    public string? WhatsappUrl { get; set; }
} 