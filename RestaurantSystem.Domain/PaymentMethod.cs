namespace RestaurantSystem.Domain;

public class PaymentMethod
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<Restaurant>? Restaurants { get; set; }
} 