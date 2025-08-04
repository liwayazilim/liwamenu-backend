using System.ComponentModel.DataAnnotations;

namespace QR_Menu.Application.Users.DTOs;

public class VerifyEmailDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;
} 