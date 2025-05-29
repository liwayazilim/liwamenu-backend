using System.ComponentModel.DataAnnotations;

namespace RestaurantSystem.Application.Users.DTOs;

public class VerifyEmailDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string VerificationCode { get; set; } = string.Empty;
} 