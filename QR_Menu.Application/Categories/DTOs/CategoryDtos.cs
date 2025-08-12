using System.ComponentModel.DataAnnotations;

namespace QR_Menu.Application.Categories.DTOs;

public class CategoryCreateDto
{
    [Required]
    public Guid RestaurantId { get; set; }
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class CategoryUpdateDto
{
    [StringLength(100)]
    public string? Name { get; set; }
    public bool? IsActive { get; set; }
}

public class CategoryReadDto
{
    public Guid Id { get; set; }
    public Guid RestaurantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int ProductsCount { get; set; }
} 