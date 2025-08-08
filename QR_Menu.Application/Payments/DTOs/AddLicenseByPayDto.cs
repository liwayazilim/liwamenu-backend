using System.ComponentModel.DataAnnotations;

namespace QR_Menu.Application.Payments.DTOs;

public class AddLicenseByPayDto
{
    public string UserEmail { get; set; } = string.Empty;
    
    public string UserPhoneNumber { get; set; } = string.Empty;
    
    public string UserBasket { get; set; } = string.Empty;
    
    // Card information
    public string CCOwner { get; set; } = string.Empty;
    
    public string CardNumber { get; set; } = string.Empty;
    
    public string ExpiryMonth { get; set; } = string.Empty;
    
    public string ExpiryYear { get; set; } = string.Empty;
    
    public string CVV { get; set; } = string.Empty;
    
    public string UserName { get; set; } = string.Empty;
    
    public string UserAddress { get; set; } = string.Empty;
    
    // Admin-specific fields (optional)
    public Guid? AdminTargetUserId { get; set; } // For admin operations
}

public class PayTRUserBasketForNewLicenseDto
{
    public string restaurantId { get; set; } = string.Empty;
    public List<string> licensePackageIds { get; set; } = new();
} 