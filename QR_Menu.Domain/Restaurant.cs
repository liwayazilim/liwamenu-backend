namespace QR_Menu.Domain;

public class Restaurant
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? DealerId { get; set; }
    public Guid? LicenseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string? Neighbourhood { get; set; }
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsActive { get; set; } = true;
    public string? WorkingHours { get; set; }
    public ICollection<PaymentMethod>? PaymentMethods { get; set; }
    public double? MinDistance { get; set; }
    public string? GoogleAnalytics { get; set; }
    public string? DefaultLang { get; set; }
    public bool InPersonOrder { get; set; } = true;
    public bool OnlineOrder { get; set; } = true;
    public string? Slogan1 { get; set; }
    public string? Slogan2 { get; set; }
    public bool Hide { get; set; } = false;
    public string? SocialLinks { get; set; }
    public int ThemeId { get; set; } = 0;
    
    // Image properties
    public byte[]? ImageData { get; set; }
    public string? ImageFileName { get; set; }
    public string? ImageContentType { get; set; }
    
    public DateTime CreatedDateTime { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdateDateTime { get; set; } = DateTime.UtcNow;
    public User? User { get; set; }
    public License? License { get; set; }
    public ICollection<Category>? Categories { get; set; }
    public ICollection<Product>? Products { get; set; }
} 