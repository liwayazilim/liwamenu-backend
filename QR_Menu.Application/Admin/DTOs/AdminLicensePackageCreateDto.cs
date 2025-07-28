using System.ComponentModel.DataAnnotations;

namespace QR_Menu.Application.Admin.DTOs;

public class AdminLicensePackageCreateDto
{
    [Required]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public Guid EntityGuid { get; set; }
    
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Time must be greater than 0")]
    public int Time { get; set; }
    
    [Required]
    [Range(0, 1, ErrorMessage = "TimeId must be 0 (month) or 1 (year)")]
    public int TimeId { get; set; } = 0; // 0 = month, 1 = year
    
    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "User price must be non-negative")]
    public double UserPrice { get; set; }
    
    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Dealer price must be non-negative")]
    public double DealerPrice { get; set; }
    
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
} 