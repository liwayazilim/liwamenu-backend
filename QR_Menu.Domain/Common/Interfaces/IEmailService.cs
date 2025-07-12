namespace QR_Menu.Domain.Common.Interfaces;
 
public interface IEmailService
{
    Task SendVerificationEmailAsync(string email, string token);
    Task SendPasswordResetEmailAsync(string email, string token);
} 