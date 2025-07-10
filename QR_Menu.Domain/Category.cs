namespace QR_Menu.Domain;

public class Category
{
    public Guid Id { get; set; }
    public Guid RestaurantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public Restaurant? Restaurant { get; set; }
    public ICollection<Product>? Products { get; set; }
} 