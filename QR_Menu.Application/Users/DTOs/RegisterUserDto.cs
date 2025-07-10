using System.ComponentModel.DataAnnotations;

namespace QR_Menu.Application.Users.DTOs;

public class RegisterUserDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [Phone]
    public string Tel { get; set; } = string.Empty;

    public string? City { get; set; }
    public string? District { get; set; }
    public string? Neighbourhood { get; set; }
} 