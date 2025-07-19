namespace QR_Menu.Application.Admin.DTOs;

public class AdminCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int ProductsCount { get; set; }
    public int ActiveProductsCount { get; set; }
} 