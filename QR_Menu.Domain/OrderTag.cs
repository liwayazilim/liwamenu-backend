namespace QR_Menu.Domain;

public class OrderTag
{
    public Guid Id { get; set; }
    public Guid RestaurantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
   
    // Audit fields
    public DateTime CreatedDateTime { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdateDateTime { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Restaurant? Restaurant { get; set; }
    public ICollection<Order>? Orders { get; set; }
    public ICollection<OrderItem>? OrderItems { get; set; }
} 