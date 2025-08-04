using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QR_Menu.Domain.Common.Interfaces;
using System.Collections.Concurrent;

namespace QR_Menu.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly string _smtpServer;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly string _fromEmail;

    // Thread-safe code stores
    private static readonly ConcurrentDictionary<string, (string Code, DateTime Expiry)> _verificationCodes = new();
    private static readonly ConcurrentDictionary<string, (string Code, DateTime Expiry)> _resetCodes = new();

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _smtpServer = _configuration["EmailSettings:SmtpServer"] ?? throw new ArgumentNullException("SmtpServer");
        _smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
        _smtpUsername = _configuration["EmailSettings:SmtpUsername"] ?? throw new ArgumentNullException("SmtpUsername");
        _smtpPassword = _configuration["EmailSettings:SmtpPassword"] ?? throw new ArgumentNullException("SmtpPassword");
        _fromEmail = _configuration["EmailSettings:FromEmail"] ?? throw new ArgumentNullException("FromEmail");
    }

    public async Task SendVerificationEmailAsync(string email, string token)
    {
        var subject = "Email Verification";
        var intro = "Thank you for registering with QR_Menu! Please click the link below to verify your email.";
        var verificationLink = $"http://localhost:9006/verify-email?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
        string htmlBody = $@"<html><body><p>Hello,</p><p>{intro}</p><p><a href='{verificationLink}'>Verify your email</a></p><p>If you did not request this, you can ignore this email.</p><p>Best regards,<br/>The QR_Menu Team</p></body></html>";
        await SendEmailAsync(email, subject, htmlBody);
    }

    public async Task SendPasswordResetEmailAsync(string email, string token)
    {
        var subject = "Password Reset";
        var intro = "You requested a password reset for QR_Menu. Please click the link below to reset your password.";
        var resetLink = $"http://localhost:9006/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
        string htmlBody = $@"<html><body><p>Hello,</p><p>{intro}</p><p><a href='{resetLink}'>Reset your password</a></p><p>If you did not request this, you can ignore this email.</p><p>Best regards,<br/>The QR_Menu Team</p></body></html>";
        await SendEmailAsync(email, subject, htmlBody);
    }

    private async Task SendEmailAsync(string email, string subject, string htmlBody)
    {
        try
        {
            _logger.LogInformation($"Attempting to send {subject} email to {email}.");
            using var client = new SmtpClient(_smtpServer, _smtpPort)
            {
                Credentials = new System.Net.NetworkCredential(_smtpUsername, _smtpPassword),
                EnableSsl = true,
                UseDefaultCredentials = false
            };
            var fromAddress = new MailAddress(_fromEmail, "QR_Menu");
            var message = new MailMessage
            {
                From = fromAddress,
                Subject = subject,
                IsBodyHtml = true,
                Body = htmlBody
            };
            message.To.Add(email);
            await client.SendMailAsync(message);
            _logger.LogInformation($"{subject} email sent to {email} successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send {subject} email to {email}.");
            throw;
        }
    }
} 