using System.ComponentModel.DataAnnotations;
using QR_Menu.Domain;
namespace QR_Menu.Application.Orders;

public class OrderCreateDto
{
    [Required]
    public Guid RestaurantId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerTel { get; set; }
    public OrderType OrderType { get; set; } = OrderType.InPerson;
    [Required]
    public List<OrderCreateItemDto> Items { get; set; } = new();
    public string? CustomerNote { get; set; }
}

public class OrderCreateItemDto
{
    [Required]
    public Guid ProductId { get; set; }
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;
    public string? OptionsJson { get; set; }
}

public class OrderReadDto
{
    public Guid Id { get; set; }
    public Guid RestaurantId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public OrderStatus Status { get; set; }
    public OrderType OrderType { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerTel { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerAddress { get; set; }
    public string? CustomerNote { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "TRY";
    public DateTime? EstimatedReadyTime { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public List<OrderItemReadDto> Items { get; set; } = new();
    
    // Computed properties
    public bool IsCompleted => Status == OrderStatus.Completed;
    public bool IsCancelled => Status == OrderStatus.Cancelled;
    public bool IsActive => !IsCompleted && !IsCancelled;
    public TimeSpan? ProcessingTime => CompletedAt?.Subtract(CreatedAt);
}

public class OrderItemReadDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
    public string? OptionsJson { get; set; }
}

public class OrderUpdateStatusDto
{
    [Required]
    public string Status { get; set; } = string.Empty; // Pending, Preparing, Ready, Completed, Cancelled
} 