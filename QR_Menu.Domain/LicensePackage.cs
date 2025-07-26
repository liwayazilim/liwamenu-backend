using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QR_Menu.Domain;

public class LicensePackage
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }
    public Guid EntityGuid { get; set; }
    public int LicenseTypeId { get; set; }
    public int Time { get; set; }
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