using Microsoft.AspNetCore.Mvc;
using QR_Menu.Application.Users;
using QR_Menu.Application.Users.DTOs;
using QR_Menu.Application.Common;

namespace QR_Menu.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmailController : ControllerBase
{
    private readonly UserService _userService;

    public EmailController(UserService userService)
    {
        _userService = userService;
    }

    [HttpPost("send-verification")]
    public async Task<ActionResult<ResponsBase>> SendVerification([FromBody] SendVerificationEmailRequest request)
    {
        var (success, message) = await _userService.ForgotPasswordAsync(request.Email);
        if (!success) return BadRequest(ResponsBase.Create(message, message, "400"));
        return Ok(ResponsBase.Create("Doğrulama e-postası gönderildi", "Verification email sent", "200"));
    }

    [HttpPost("send-password-reset")]
    public async Task<ActionResult<ResponsBase>> SendPasswordReset([FromBody] ForgotPasswordDto dto)
    {
        var (success, message) = await _userService.ForgotPasswordAsync(dto.EmailOrPhone);
        if (!success) return BadRequest(ResponsBase.Create(message, message, "400"));
        return Ok(ResponsBase.Create("Şifre sıfırlama e-postası gönderildi", "Password reset email sent", "200"));
    }
}

public class SendVerificationEmailRequest
{
    public string Email { get; set; } = string.Empty;
} 