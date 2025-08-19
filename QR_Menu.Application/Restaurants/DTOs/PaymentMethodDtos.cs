namespace QR_Menu.Application.Restaurants.DTOs;

using System.ComponentModel.DataAnnotations;

public class PaymentMethodOptionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}

public class PaymentMethodsUpdateDto
{
    [Required]
    public Guid RestaurantId { get; set; }
    [Required]
    public List<Guid> MethodIds { get; set; } = new();
}

// New DTO for adding payment methods to restaurants
public class AddPaymentMethodToRestaurantDto
{
    [Required]
    public Guid RestaurantId { get; set; }
    
    [Required]
    [StringLength(100, ErrorMessage = "Payment method name cannot exceed 100 characters")]
    public string PaymentMethodName { get; set; } = string.Empty;
} 