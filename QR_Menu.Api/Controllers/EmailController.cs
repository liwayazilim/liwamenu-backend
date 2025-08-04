using Microsoft.AspNetCore.Mvc;
using QR_Menu.Application.Users;
using QR_Menu.Application.Users.DTOs;
using QR_Menu.Application.Common;
using System.Net;

namespace QR_Menu.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmailController : BaseController
{
    private readonly UserService _userService;

    public EmailController(UserService userService)
    {
        _userService = userService;
    }

    [HttpPost("send-verification")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResponsBase>> SendVerification([FromBody] SendVerificationEmailRequest request)
    {
        var (success, message) = await _userService.ResendEmailVerificationAsync(request.Email);
        if (!success) return BadRequest(message, message);
        return Success("Doğrulama e-postası gönderildi", "Verification email sent");
    }

    [HttpPost("send-password-reset")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResponsBase>> SendPasswordReset([FromBody] ForgotPasswordDto dto)
    {
        var (success, message) = await _userService.ForgotPasswordAsync(dto.EmailOrPhone);
        if (!success) return BadRequest(message, message);
        return Success("Şifre sıfırlama e-postası gönderildi", "Password reset email sent");
    }
}

public class SendVerificationEmailRequest
{
    public string Email { get; set; } = string.Empty;
} 