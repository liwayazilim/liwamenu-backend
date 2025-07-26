using System.ComponentModel.DataAnnotations;

namespace QR_Menu.Application.Admin.DTOs;

public class AdminLicensePackageCreateDto
{
    [Required]
    public Guid EntityGuid { get; set; }
    
    [Required]
    public int LicenseTypeId { get; set; }
    
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Time must be greater than 0")]
    public int Time { get; set; }
    
    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "User price must be non-negative")]
    public double UserPrice { get; set; }
    
    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Dealer price must be non-negative")]
    public double DealerPrice { get; set; }
    
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
} 