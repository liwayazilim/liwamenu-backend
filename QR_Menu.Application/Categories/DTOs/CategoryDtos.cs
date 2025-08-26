using System.ComponentModel.DataAnnotations;

namespace QR_Menu.Application.Categories.DTOs;

public class CategoryCreateDto
{
    [Required(ErrorMessage = "Restoran ID zorunludur")]
    public Guid RestaurantId { get; set; }
    
    [Required(ErrorMessage = "Kategori adı zorunludur")]
    [StringLength(100, ErrorMessage = "Kategori adı 100 karakteri geçemez")]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(10, ErrorMessage = "İkon 10 karakteri geçemez")]
    public string? Icon { get; set; }
}

public class CategoryUpdateDto
{
    [StringLength(100, ErrorMessage = "Kategori adı 100 karakteri geçemez")]
    public string? Name { get; set; }
    
    [StringLength(10, ErrorMessage = "İkon 10 karakteri geçemez")]
    public string? Icon { get; set; }
}

public class CategoryReadDto
{
    public Guid Id { get; set; }
    public Guid RestaurantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public bool IsActive { get; set; }
    public int ProductsCount { get; set; }
    public int ActiveProductsCount { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public DateTime LastUpdateDateTime { get; set; }
} 