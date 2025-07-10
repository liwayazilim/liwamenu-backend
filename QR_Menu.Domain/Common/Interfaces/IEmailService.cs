namespace QR_Menu.Domain.Common.Interfaces;
 
public interface IEmailService
{
    Task SendVerificationEmailAsync(string email, string? code = null);
    bool TryGetVerificationCode(string email, out (string Code, DateTime Expiry) data);
    bool ValidateVerificationCode(string email, string code);
    Task SendPasswordResetEmailAsync(string email, string? code = null);
    bool TryGetResetCode(string email, out (string Code, DateTime Expiry) data);
    bool ValidateResetCode(string email, string code);
    string GenerateCode();
} 