namespace QR_Menu.Application.Payments.DTOs;

public class PaymentReadDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public Guid? RestaurantId { get; set; }
    public Guid? OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    
    // PayTR specific fields
    public string? PayTRToken { get; set; }
    public string? PayTRTransactionId { get; set; }
    public string? PayTRMerchantOrderId { get; set; }
    public string? PayTRPaymentLink { get; set; }
    public string? PayTRPaymentLinkId { get; set; }
    
    // Payment details
    public string? CardMask { get; set; }
    public string? InstallmentCount { get; set; }
    public string? BasketItems { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerName { get; set; }
    
    // Error information
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    
    // Timestamps
    public DateTime CreatedDateTime { get; set; }
    public DateTime LastUpdateDateTime { get; set; }
    public DateTime? PaymentDateTime { get; set; }
    
    // Navigation properties
    public string? UserName { get; set; }
    public string? RestaurantName { get; set; }
} 