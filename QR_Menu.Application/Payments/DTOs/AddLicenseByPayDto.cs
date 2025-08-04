using System.ComponentModel.DataAnnotations;

namespace QR_Menu.Application.Payments.DTOs;

public class AddLicenseByPayDto
{
    [Required]
    public string UserEmail { get; set; } = string.Empty;
    
    [Required]
    public string UserPhoneNumber { get; set; } = string.Empty;
    
    [Required]
    public string UserBasket { get; set; } = string.Empty; // JSON string containing license packages for multiple restaurants
    
    // Card information
    [Required]
    public string CCOwner { get; set; } = string.Empty;
    
    [Required]
    public string CardNumber { get; set; } = string.Empty;
    
    [Required]
    [Range(1, 12)]
    public string ExpiryMonth { get; set; } = string.Empty;
    
    [Required]
    public string ExpiryYear { get; set; } = string.Empty;
    
    [Required]
    public string CVV { get; set; } = string.Empty;
    
    [Required]
    public string UserName { get; set; } = string.Empty;
    
    [Required]
    public string UserAddress { get; set; } = string.Empty;
    
    // Admin-specific fields (optional)
    public Guid? AdminTargetUserId { get; set; } // If provided, admin is paying for another user
}

public class PayTRUserBasketForNewLicenseDto
{
    public string restaurantId { get; set; } = string.Empty;
    public List<string> licensePackageIds { get; set; } = new();
} 