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

            var fromAddress = new MailAddress(_fromEmail, "Restaurant System");
            var message = new MailMessage
            {
                From = fromAddress,
                Subject = "Email Verification",
                IsBodyHtml = true // We'll add both views
            };
            message.To.Add(email);

            string plainTextBody = $"Hello,\n\nYour verification code is: {verificationCode}\n\nThank you for registering with Restaurant System!\n\nBest regards,\nThe Restaurant System Team";
            string htmlBody = $@"<html><body><p>Hello,</p><p>Your verification code is: <b>{verificationCode}</b></p><p>Thank you for registering with <b>Restaurant System</b>!</p><p>Best regards,<br/>The Restaurant System Team</p></body></html>";

            var plainView = AlternateView.CreateAlternateViewFromString(plainTextBody, null, "text/plain");
            var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html");
            message.AlternateViews.Add(plainView);
            message.AlternateViews.Add(htmlView);

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