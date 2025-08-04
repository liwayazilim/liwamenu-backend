using System.ComponentModel.DataAnnotations;

namespace QR_Menu.Application.Payments.DTOs;

public class PaymentCreateDto
{
    [Required]
    public Guid UserId { get; set; }
    
    public Guid? RestaurantId { get; set; }
    public Guid? OrderId { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }
    
    public string Currency { get; set; } = "TRY";
    public string InstallmentCount { get; set; } = "0";
    
    [Required]
    public string CustomerEmail { get; set; } = string.Empty;
    
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerAddress { get; set; }
    
    [Required]
    public string BasketItems { get; set; } = string.Empty; // JSON string
    
    // Card information (for direct payment)
    public string? CardHolderName { get; set; }
    public string? CardNumber { get; set; }
    public string? CardExpireMonth { get; set; }
    public string? CardExpireYear { get; set; }
    public string? CardCvc { get; set; }
    
    // Link specific fields
    public string? LinkExpireDate { get; set; }
    public string? LinkDescription { get; set; }
    public string? LinkImageUrl { get; set; }
    
    // URLs
    public string? SuccessUrl { get; set; }
    public string? FailUrl { get; set; }
} 