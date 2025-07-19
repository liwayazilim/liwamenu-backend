namespace QR_Menu.Application.Admin.DTOs;

public class UpdateOwnerDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? Neighbourhood { get; set; }
    public bool? IsActive { get; set; }
    public bool? EmailConfirmed { get; set; }
    public Guid? DealerId { get; set; } // Change assigned dealer
} 