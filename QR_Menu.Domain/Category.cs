namespace QR_Menu.Domain;

public class Category
{
    public Guid Id { get; set; }
    public Guid RestaurantId { get; set; }
    
    // Basic information
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ShortDescription { get; set; } // For display in lists
    
    // Display and ordering
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;
  
    
    // Image support
    public string? ImageFileName { get; set; }
    public string? ImageContentType { get; set; }
    public byte[]? ImageData { get; set; }
    
    // Category metadata
    public string? Color { get; set; } // For UI display (e.g., "#FF0000")
    public string? Icon { get; set; } // For UI display (e.g., "🍔", "🍕")
    public bool IsMainCategory { get; set; } = false; // For primary categories
    
    // Audit fields
    public DateTime CreatedDateTime { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdateDateTime { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Restaurant? Restaurant { get; set; }
    public ICollection<Product>? Products { get; set; }
    
    // Computed properties
    public bool HasImage => !string.IsNullOrEmpty(ImageFileName);
    public int ActiveProductsCount => Products?.Count(p => p.IsActive) ?? 0;
    public int TotalProductsCount => Products?.Count ?? 0;
} 