namespace QR_Menu.Domain;

public class License
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public double? UserPrice { get; set; }
    public double? DealerPrice { get; set; }
    public Guid? RestaurantId { get; set; }
    public Guid? LicensePackageId { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDateTime { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdateDateTime { get; set; } = DateTime.UtcNow;
    public User? User { get; set; }
    public Restaurant? Restaurant { get; set; }
    public LicensePackage? LicensePackage { get; set; }
} 