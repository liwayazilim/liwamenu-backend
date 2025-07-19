namespace QR_Menu.Application.Admin.DTOs;

public class AdminLicenseHistoryDto
{
    public DateTime Date { get; set; }
    public string Action { get; set; } = string.Empty; // Created, Activated, Deactivated, Extended, Expired
    public string? Details { get; set; }
    public string? PerformedBy { get; set; }
} 