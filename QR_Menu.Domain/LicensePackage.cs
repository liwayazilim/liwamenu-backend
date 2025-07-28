using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QR_Menu.Domain;

public class LicensePackage
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    public Guid EntityGuid { get; set; }
    public int Time { get; set; }
    
    [Range(0, 1, ErrorMessage = "TimeId must be 0 (month) or 1 (year)")]
    public int TimeId { get; set; } = 0; // 0 = month, 1 = year
    
    public double UserPrice { get; set; }
    public double DealerPrice { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDateTime { get; set; }

    [ConcurrencyCheck]
    public DateTime LastUpdateDateTime { get; set; }

    // Navigation properties
    public ICollection<License>? Licenses { get; set; }
} 