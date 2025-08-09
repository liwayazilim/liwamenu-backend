using System.ComponentModel.DataAnnotations;

namespace QR_Menu.Application.Users.DTOs;

public class UpdatePasswordDto
{
    [Required]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
    public string NewPasswordConfirm { get; set; } = string.Empty;
} 