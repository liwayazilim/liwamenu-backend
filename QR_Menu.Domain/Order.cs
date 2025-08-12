namespace QR_Menu.Domain;

public enum OrderStatus
{
    Pending,
    Preparing,
    Ready,
    Completed,
    Cancelled
}

public class Order
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid RestaurantId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public string? CustomerName { get; set; }
    public string? CustomerTel { get; set; }
    public bool IsInPerson { get; set; }
    public ICollection<OrderItem>? Items { get; set; }
    public User? User { get; set; }
    public Restaurant? Restaurant { get; set; }
} 
