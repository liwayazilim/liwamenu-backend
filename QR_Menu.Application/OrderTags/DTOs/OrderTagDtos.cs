using System.ComponentModel.DataAnnotations;

namespace QR_Menu.Application.OrderTags;

public class OrderTagCreateDto
{
    [Required(ErrorMessage = "Restoran ID zorunludur")]
    public Guid RestaurantId { get; set; }
    
    [Required(ErrorMessage = "Etiket adı zorunludur")]
    [StringLength(100, ErrorMessage = "Etiket adı 100 karakteri geçemez")]
    public string Name { get; set; } = string.Empty;
    
    [Range(0, 999999, ErrorMessage = "Fiyat 0-999999 arasında olmalıdır")]
    public decimal Price { get; set; }
}

public class OrderTagUpdateDto
{
    [StringLength(100, ErrorMessage = "Etiket adı 100 karakteri geçemez")]
    public string? Name { get; set; }
    
    [Range(0, 999999, ErrorMessage = "Fiyat 0-999999 arasında olmalıdır")]
    public decimal? Price { get; set; }
}

public class OrderTagReadDto
{
    public Guid Id { get; set; }
    public Guid RestaurantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public DateTime LastUpdateDateTime { get; set; }
}

public class BulkOrderTagCreateDto
{
    [Required(ErrorMessage = "Restoran ID zorunludur")]
    public Guid RestaurantId { get; set; }
    
    [Required(ErrorMessage = "En az bir etiket gerekli")]
    [MinLength(1, ErrorMessage = "En az bir etiket gerekli")]
    public List<OrderTagCreateDto> Tags { get; set; } = new();
}

public class BulkOrderTagUpdateDto
{
    [Required(ErrorMessage = "En az bir etiket gerekli")]
    [MinLength(1, ErrorMessage = "En az bir etiket gerekli")]
    public List<OrderTagUpdateItemDto> Tags { get; set; } = new();
}

public class OrderTagUpdateItemDto
{
    [Required(ErrorMessage = "Etiket ID zorunludur")]
    public Guid Id { get; set; }
    
    [StringLength(100, ErrorMessage = "Etiket adı 100 karakteri geçemez")]
    public string? Name { get; set; }
    
    [Range(0, 999999, ErrorMessage = "Fiyat 0-999999 arasında olmalıdır")]
    public decimal? Price { get; set; }
} 