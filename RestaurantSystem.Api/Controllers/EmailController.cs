using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RestaurantSystem.Domain.Common.Interfaces;

namespace RestaurantSystem.Api.Controllers;

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
        await _emailService.SendVerificationEmailAsync(request.Email, request.Code);
        return Ok(new { message = "Verification email sent." });
    }

    // TODO: Add email-related endpoints (send verification, password reset, etc.)
}

public class SendVerificationEmailRequest
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
} 