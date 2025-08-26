namespace QR_Menu.Domain;

public class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }

    // Product snapshot (preserved for historical accuracy)
    public string ProductNameSnapshot { get; set; } = string.Empty;
    public string? ProductDescriptionSnapshot { get; set; }
    public string? ProductImageSnapshot { get; set; }
    
    // Pricing information
    public decimal UnitPrice { get; set; }
    public decimal DiscountedUnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal FinalLineTotal { get; set; }
    
   
    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdateDateTime { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Order? Order { get; set; }
    public Product? Product { get; set; }
    public ICollection<OrderTag>? Tags { get; set; }
    
    // Computed properties
    public decimal TotalDiscount => (UnitPrice - DiscountedUnitPrice) * Quantity;
    public decimal TotalWithTax => FinalLineTotal + TaxAmount;
} 