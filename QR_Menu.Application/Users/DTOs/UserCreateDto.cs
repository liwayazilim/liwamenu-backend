namespace QR_Menu.Application.Users.DTOs;

public class UserCreateDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // Manager, Dealer, Owner, Customer
    public bool IsActive { get; set; } = true;
    public bool IsDealer { get; set; } = false;
    public string? City { get; set; }
    public string? District { get; set; }
    public string? Neighbourhood { get; set; }
    public Guid? DealerId { get; set; }
    public string? Note { get; set; }
    public bool SendSMSNotify { get; set; } = true;
    public bool SendEmailNotify { get; set; } = true;
} 