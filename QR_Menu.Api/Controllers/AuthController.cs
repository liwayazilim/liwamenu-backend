using Microsoft.AspNetCore.Mvc;
using QR_Menu.Application.Users;
using QR_Menu.Application.Users.DTOs;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace QR_Menu.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserService _userService;
    private readonly IConfiguration _config;

    public AuthController(UserService userService, IConfiguration config)
    {
        _userService = userService;
        _config = config;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
    {
        var (success, message) = await _userService.RegisterAsync(dto);
        if (!success)
            return BadRequest(new { message });
        return Ok(new { message });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUserDto dto)
    {
        var (success, message, user) = await _userService.LoginAsync(dto.EmailOrPhone, dto.Password);
        if (!success || user == null)
            return BadRequest(new { message });

        // Generate JWT
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddDays(7),
            signingCredentials: creds
        );
        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return Ok(new { token = jwt });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (userIdStr == null || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized(new { message = "Invalid user." });
        var (success, message) = await _userService.ChangePasswordAsync(userId, dto.CurrentPassword, dto.NewPassword);
        if (!success)
            return BadRequest(new { message });
        return Ok(new { message });
    }
} 