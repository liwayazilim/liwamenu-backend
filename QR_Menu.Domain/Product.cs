namespace QR_Menu.Domain;

public class Product
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public Guid RestaurantId { get; set; }
    
    // Basic information
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ShortDescription { get; set; } // For display in lists
    
    // Pricing
    public decimal Price { get; set; }
    
    
    // Product details
    public string? ImageFileName { get; set; }
    public string? ImageContentType { get; set; }
    public byte[]? ImageData { get; set; }
  
    
    // Inventory and availability
    public bool IsActive { get; set; } = true;
    public bool IsAvailable { get; set; } = true;
    public int? StockQuantity { get; set; } // null = unlimited
    public int? MinOrderQuantity { get; set; } = 1;
    public int? MaxOrderQuantity { get; set; } // null = unlimited
    
    // Display and ordering
    public int DisplayOrder { get; set; } = 0;
    public bool IsFeatured { get; set; } = false;
    public bool IsPopular { get; set; } = false;
    
    // Audit fields
    public DateTime CreatedDateTime { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdateDateTime { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Category? Category { get; set; }
    public Restaurant? Restaurant { get; set; }
    public ICollection<OrderItem>? OrderItems { get; set; }
  
} 