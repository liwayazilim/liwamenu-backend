using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using QR_Menu.Domain.Common.Interfaces;
using QR_Menu.Application.Users.DTOs;

namespace QR_Menu.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmailController : ControllerBase
{
    private readonly IEmailService _emailService;

    public EmailController(IEmailService emailService)
    {
        _emailService = emailService;
    }

    [HttpPost("send-verification")]
    public async Task<IActionResult> SendVerification([FromBody] SendVerificationEmailRequest request)
    {
        await _emailService.SendVerificationEmailAsync(request.Email);
        return Ok(new { message = "Verification email sent." });
    }

    [HttpPost("verify-email")]
    public IActionResult VerifyEmail([FromBody] VerifyEmailDto dto)
    {
        var valid = _emailService.ValidateVerificationCode(dto.Email, dto.VerificationCode);
        if (!valid)
            return BadRequest(new { message = "Invalid or expired verification code" });
        return Ok(new { message = "Email verified successfully" });
    }

    [HttpPost("send-password-reset")]
    public async Task<IActionResult> SendPasswordReset([FromBody] ForgotPasswordDto dto)
    {
        await _emailService.SendPasswordResetEmailAsync(dto.EmailOrPhone);
        return Ok(new { message = "Password reset code sent to your email." });
    }

    [HttpPost("verify-password-reset")]
    public IActionResult VerifyPasswordReset([FromBody] ResetPasswordDto dto)
    {
        var valid = _emailService.ValidateResetCode(dto.EmailOrPhone, dto.Code);
        if (!valid)
            return BadRequest(new { message = "Invalid or expired reset code" });
        return Ok(new { message = "Reset code is valid." });
    }
}

public class SendVerificationEmailRequest
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
} 