using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using QR_Menu.Domain.Common.Interfaces;
using QR_Menu.Application.Users.DTOs;
using QR_Menu.Application.Users;

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
    public async Task<IActionResult> SendVerification([FromBody] SendVerificationEmailRequest request)
    {
        // Registration now sends verification automatically, but you can re-send if needed
        var user = await _userService.GetByEmailAsync(request.Email);
        if (user == null)
            return NotFound(new { message = "User not found." });
        var token = await _userService.GenerateEmailConfirmationTokenAsync(user.Id);
        await _userService.SendVerificationEmailAsync(user.Email, token);
        return Ok(new { message = "Verification email sent." });
    }

    [HttpPost("send-password-reset")]
    public async Task<IActionResult> SendPasswordReset([FromBody] ForgotPasswordDto dto)
    {
        var (success, message) = await _userService.ForgotPasswordAsync(dto.EmailOrPhone);
        if (!success)
            return BadRequest(new { message });
        return Ok(new { message });
    }
}

public class SendVerificationEmailRequest
{
    public string Email { get; set; } = string.Empty;
} 