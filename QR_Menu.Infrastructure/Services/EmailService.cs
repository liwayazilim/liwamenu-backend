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

    public string GenerateCode()
    {
        return new Random().Next(1000, 9999).ToString();
    }

    // --- Verification Email Logic ---
    public async Task SendVerificationEmailAsync(string email, string? code = null)
    {
        if (string.IsNullOrEmpty(code))
            code = GenerateCode();
        _verificationCodes[email] = (code, DateTime.UtcNow.AddMinutes(15));
        await SendEmailAsync(email, code, "Email Verification", "Thank you for registering with QR_Menu!");
    }

    public bool TryGetVerificationCode(string email, out (string Code, DateTime Expiry) data)
    {
        return _verificationCodes.TryGetValue(email, out data);
    }

    public bool ValidateVerificationCode(string email, string code)
    {
        if (!_verificationCodes.TryGetValue(email, out var data))
            return false;
        if (DateTime.UtcNow > data.Expiry)
        {
            _verificationCodes.TryRemove(email, out _);
            return false;
        }
        if (data.Code != code)
            return false;
        _verificationCodes.TryRemove(email, out _);
        return true;
    }

    // --- Password Reset Email Logic ---
    public async Task SendPasswordResetEmailAsync(string email, string? code = null)
    {
        if (string.IsNullOrEmpty(code))
            code = GenerateCode();
        _resetCodes[email] = (code, DateTime.UtcNow.AddMinutes(15));
        await SendEmailAsync(email, code, "Password Reset", "You requested a password reset for QR_Menu.");
    }

    public bool TryGetResetCode(string email, out (string Code, DateTime Expiry) data)
    {
        return _resetCodes.TryGetValue(email, out data);
    }

    public bool ValidateResetCode(string email, string code)
    {
        if (!_resetCodes.TryGetValue(email, out var data))
            return false;
        if (DateTime.UtcNow > data.Expiry)
        {
            _resetCodes.TryRemove(email, out _);
            return false;
        }
        if (data.Code != code)
            return false;
        _resetCodes.TryRemove(email, out _);
        return true;
    }

    // --- Email Sending Helper ---
    private async Task SendEmailAsync(string email, string code, string subject, string intro)
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
                IsBodyHtml = true
            };
            message.To.Add(email);
            string plainTextBody = $"Hello,\n\nYour code is: {code}\n\n{intro}\n\nBest regards,\nThe QR_Menu Team";
            string htmlBody = $@"<html><body><p>Hello,</p><p>Your code is: <b>{code}</b></p><p>{intro}</p><p>Best regards,<br/>The QR_Menu Team</p></body></html>";
            var plainView = AlternateView.CreateAlternateViewFromString(plainTextBody, null, "text/plain");
            var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html");
            message.AlternateViews.Add(plainView);
            message.AlternateViews.Add(htmlView);
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