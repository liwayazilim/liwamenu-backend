namespace QR_Menu.Domain;

public enum OrderStatus
{
    Pending,
    Preparing,
    Ready,
    Completed,
    Cancelled
}

public enum OrderType
{
    InPerson,
    Takeaway
    
}

public class Order
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid RestaurantId { get; set; }
    
    // Order details
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public OrderType OrderType { get; set; } = OrderType.InPerson;
    
    // Customer information
    public string? CustomerName { get; set; }
    public string? CustomerTel { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerAddress { get; set; }
    public string? CustomerNote { get; set; }
    
    // Financial information
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "TRY";
    
    // Timing information
    public DateTime? EstimatedReadyTime { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    
    // Audit fields
    public DateTime CreatedDateTime { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdateDateTime { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public User? User { get; set; }
    public Restaurant? Restaurant { get; set; }
    public ICollection<OrderItem>? Items { get; set; }
    public ICollection<OrderTag>? Tags { get; set; }
    
    // Computed properties
    public bool IsCompleted => Status == OrderStatus.Completed;
    public bool IsCancelled => Status == OrderStatus.Cancelled;
    public bool IsActive => !IsCompleted && !IsCancelled;
    public TimeSpan? ProcessingTime => CompletedAt?.Subtract(CreatedAt);
} 
