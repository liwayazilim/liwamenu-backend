namespace QR_Menu.Domain;

public enum TagType
{
    OrderLevel,    // Applies to entire order (e.g., "Takeaway", "in-resto")
    ItemLevel      // Applies to specific items (e.g "No Onions", "Extra Cheese")
}

public class OrderTag
{
    public Guid Id { get; set; }
    public Guid RestaurantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TagType TagType { get; set; } = TagType.ItemLevel;
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; } = 0; // For UI ordering
    public string? Color { get; set; } // For UI display (e.g., "#FF0000")
    public string? Icon { get; set; } // For UI display (e.g., "spicy", "vegetarian")
    
    // Audit fields
    public DateTime CreatedDateTime { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdateDateTime { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Restaurant? Restaurant { get; set; }
    public ICollection<Order>? Orders { get; set; }
    public ICollection<OrderItem>? OrderItems { get; set; }
} 