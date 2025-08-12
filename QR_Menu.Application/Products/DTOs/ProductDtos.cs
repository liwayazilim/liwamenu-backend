using System.ComponentModel.DataAnnotations;

namespace QR_Menu.Application.Products.DTOs;

public class ProductCreateDto
{
    [Required]
    public Guid RestaurantId { get; set; }
    [Required]
    public Guid CategoryId { get; set; }
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    [Range(0, 999999)]
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ProductUpdateDto
{
    public Guid? CategoryId { get; set; }
    [StringLength(200)]
    public string? Name { get; set; }
    public string? Description { get; set; }
    [Range(0, 999999)]
    public decimal? Price { get; set; }
    public bool? IsActive { get; set; }
}

public class ProductReadDto
{
    public Guid Id { get; set; }
    public Guid RestaurantId { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
} 