using QR_Menu.Domain;

namespace QR_Menu.Application.Admin.DTOs;

public class AdminOrderSummaryDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? CustomerTel { get; set; }
    public OrderType OrderType { get; set; }
    public int ProductsCount { get; set; }
    public decimal TotalAmount { get; set; }
} 