namespace QR_Menu.Domain;

public class PaymentMethod
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDateTime { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdateDateTime { get; set; } = DateTime.UtcNow;
    public ICollection<Restaurant>? Restaurants { get; set; }
} 