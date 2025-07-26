namespace QR_Menu.Application.Users.DTOs;

public class UserUpdateDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Role { get; set; } // Admin, Dealer, Owner, Customer
    public bool? IsActive { get; set; }
    public bool? IsDealer { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? Neighbourhood { get; set; }
    public Guid? DealerId { get; set; }
    public string? Note { get; set; }
    public string? PassiveNote { get; set; } // Note when user is deactivated
    public bool? SendSMSNotify { get; set; }
    public bool? SendEmailNotify { get; set; }
} 