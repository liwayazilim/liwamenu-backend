using System.ComponentModel.DataAnnotations;

namespace QR_Menu.Domain;

public enum PaymentStatus
{
    Waiting,
    Success,
    Failed,
    Cancelled,
    Refunded
}

public enum PaymentType
{
    CreditCard,
    BankTransfer,
    PayTR
}

public enum PaymentLicenseType
{
    NewLicense,
    ExtendLicense,
    Link
}

public class Payment
{
    public Guid Id { get; set; }
    
    [Required]
    public string OrderNumber { get; set; } = string.Empty;
    
    public Guid UserId { get; set; }
    public Guid? RestaurantId { get; set; }
    public Guid? OrderId { get; set; }
    
    [Required]
    public decimal Amount { get; set; }
    
    [Required]
    public string Currency { get; set; } = "TRY";
    
    public PaymentType PaymentMethod { get; set; } = PaymentType.PayTR;
    public PaymentStatus Status { get; set; } = PaymentStatus.Waiting;
    public PaymentLicenseType? LicenseType { get; set; } // Type of license operation
    
    // PayTR specific fields
    public string? PayTRToken { get; set; }
    public string? PayTRTransactionId { get; set; }
    public string? PayTRMerchantOrderId { get; set; }
    public string? PayTRCallbackToken { get; set; }
    public string? PayTRPaymentLink { get; set; }
    public string? PayTRPaymentLinkId { get; set; }
    
    // Payment details
    public string? CardMask { get; set; }
    public string? InstallmentCount { get; set; }
    public string? BasketItems { get; set; } // JSON string of basket items
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerName { get; set; }
    
    // Error information
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    
    // Timestamps
    public DateTime CreatedDateTime { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdateDateTime { get; set; } = DateTime.UtcNow;
    public DateTime? PaymentDateTime { get; set; }
    
    // Navigation properties
    public User? User { get; set; }
    public Restaurant? Restaurant { get; set; }
    public Order? Order { get; set; }
} 