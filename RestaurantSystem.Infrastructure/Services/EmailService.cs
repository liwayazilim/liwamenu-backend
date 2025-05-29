using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RestaurantSystem.Domain.Common.Interfaces;

namespace RestaurantSystem.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly string _smtpServer;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly string _fromEmail;

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

    public async Task SendVerificationEmailAsync(string email, string verificationCode)
    {
        try
        {
            _logger.LogInformation($"Attempting to send verification email to {email}.");
            using var client = new SmtpClient(_smtpServer, _smtpPort)
            {
                Credentials = new System.Net.NetworkCredential(_smtpUsername, _smtpPassword),
                EnableSsl = true,
                UseDefaultCredentials = false
            };

            var message = new MailMessage
            {
                From = new MailAddress(_fromEmail),
                Subject = "Email Verification",
                Body = $"Your verification code is: {verificationCode}",
                IsBodyHtml = false
            };
            message.To.Add(email);

            await client.SendMailAsync(message);
            _logger.LogInformation($"Verification email sent to {email} successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send verification email to {email}.");
            throw;
        }
    }
} 