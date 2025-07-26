using System.ComponentModel.DataAnnotations;

namespace QR_Menu.Application.Admin.DTOs;

public class AdminLicensePackageUpdateDto
{
    public Guid? EntityGuid { get; set; }
    
    [Range(1, int.MaxValue, ErrorMessage = "License type ID must be greater than 0")]
    public int? LicenseTypeId { get; set; }
    
    [Range(1, int.MaxValue, ErrorMessage = "Time must be greater than 0")]
    public int? Time { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "User price must be non-negative")]
    public double? UserPrice { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Dealer price must be non-negative")]
    public double? DealerPrice { get; set; }
    
    public string? Description { get; set; }
    
    public bool? IsActive { get; set; }
} 