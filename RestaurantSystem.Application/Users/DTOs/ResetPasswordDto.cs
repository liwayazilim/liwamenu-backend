namespace RestaurantSystem.Application.Users.DTOs;

public class ResetPasswordDto
{
    public string EmailOrPhone { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
} 