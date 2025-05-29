namespace RestaurantSystem.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string email, string verificationCode);
} 