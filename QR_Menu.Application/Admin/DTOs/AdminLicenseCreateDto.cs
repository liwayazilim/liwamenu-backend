using System.ComponentModel.DataAnnotations;

namespace QR_Menu.Application.Admin.DTOs;

public class AdminLicenseCreateDto
{
    [Required]
    public Guid RestaurantId { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    public Guid LicensePackageId { get; set; }
    
    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "License type ID must be non-negative")]
    public int LicenseTypeId { get; set; }
    
    [Required]
    public DateTime StartDateTime { get; set; }
    
    // EndDateTime is now optional - it will be calculated automatically
    public DateTime? EndDateTime { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    [Range(0, int.MaxValue, ErrorMessage = "License package time must be non-negative")]
    public int LicensePackageTime { get; set; } = 0;
    
    [Range(0, int.MaxValue, ErrorMessage = "License package total price must be non-negative")]
    public int LicensePackageTotalPrice { get; set; } = 0;
} 