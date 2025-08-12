using System.ComponentModel.DataAnnotations;

namespace QR_Menu.Application.Orders.DTOs;

public class OrderCreateDto
{
    [Required]
    public Guid RestaurantId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerTel { get; set; }
    public bool IsInPerson { get; set; } = true;
    [Required]
    public List<OrderCreateItemDto> Items { get; set; } = new();
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
    public string Status { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? CustomerTel { get; set; }
    public bool IsInPerson { get; set; }
    public decimal TotalAmount { get; set; }
    public List<OrderItemReadDto> Items { get; set; } = new();
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