namespace RestaurantSystem.Application.Users.DTOs;

public class UserCreateDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Tel { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // Admin, Dealer, Owner, Customer
    public bool IsActive { get; set; } = true;
    public bool IsDealer { get; set; } = false;
    public string? City { get; set; }
    public string? District { get; set; }
    public string? Neighbourhood { get; set; }
} 