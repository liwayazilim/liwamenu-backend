using System.ComponentModel.DataAnnotations;

namespace QR_Menu.Application.Payments.DTOs;

public class CreatePaymentLinkDto
{
    [Required]
    public string Products { get; set; } = string.Empty;
    
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Total price must be greater than 0")]
    public double? TotalPrice { get; set; }
    
    [Range(0, 12, ErrorMessage = "Installment must be between 0 and 12")]
    public int Installment { get; set; } = 0;
    
    [Range(1, int.MaxValue, ErrorMessage = "Stock quantity must be at least 1")]
    public int StockQuantity { get; set; } = 1;
    
    [Required]
    public DateTime ExpiryDate { get; set; }
    
    public bool CreateQR { get; set; } = false;
}

public class DeletePaymentLinkDto
{
    [Required]
    public string Id { get; set; } = string.Empty;
}

public class PaymentBasketDto
{
    public Guid RestaurantId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public object? Licenses { get; set; } // Can be single license or list of licenses
}

public class PaymentBasketLicenseDto
{
    public Guid LicenseId { get; set; }
    public Guid LicensePackageId { get; set; }
    public string LicensePackageName { get; set; } = string.Empty;
    public int LicensePackageTypeId { get; set; }
    public int LicensePackageTime { get; set; }
    public double LicensePackagePrice { get; set; }
} 