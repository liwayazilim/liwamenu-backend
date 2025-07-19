namespace QR_Menu.Application.Users.DTOs;

public class BulkStatusUpdateDto
{
    public List<Guid> Ids { get; set; } = new();
    public bool? IsActive { get; set; }
} 