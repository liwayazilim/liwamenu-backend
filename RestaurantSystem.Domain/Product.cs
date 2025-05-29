namespace RestaurantSystem.Domain;

public class Product
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public Guid RestaurantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    public Category? Category { get; set; }
    public Restaurant? Restaurant { get; set; }
    public ICollection<Order>? Orders { get; set; }
} 