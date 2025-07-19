using Microsoft.AspNetCore.Mvc;
using QR_Menu.Application.Users;
using QR_Menu.Application.Users.DTOs;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using QR_Menu.Domain;
using System.Net;

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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
    {
        var (success, message) = await _userService.RegisterAsync(dto);
        if (!success)
            return BadRequest(new { message });
        return Ok(new { message });
    }

    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginUserDto dto)
    {
        var (success, message, user) = await _userService.LoginAsync(dto.EmailOrPhone, dto.Password);
        if (!success || user == null)
            return BadRequest(new { message });

        // Get user manager for role information
        var userManager = HttpContext.RequestServices.GetRequiredService<UserManager<User>>();
        var identityUser = await userManager.FindByIdAsync(user.Id.ToString());
        var userRoles = await userManager.GetRolesAsync(identityUser!);

        // Generate JWT with comprehensive claims
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim("firstName", user.FirstName),
            new Claim("lastName", user.LastName),
            new Claim("isActive", user.IsActive.ToString().ToLower()),
            new Claim("emailConfirmed", user.EmailConfirmed.ToString().ToLower()),
            new Claim("isDealer", user.IsDealer.ToString().ToLower())
        };

        // Add all user roles to claims
        foreach (var role in userRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Add legacy role for backward compatibility
        claims.Add(new Claim("legacyRole", user.Role.ToString()));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8), // Shorter token lifetime for better security
            signingCredentials: creds
        );
        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        
        return Ok(new { 
            token = jwt, 
            user = new {
                user.Id,
                user.FirstName,
                user.LastName,
                user.FullName,
                user.Email,
                user.Role,
                user.IsActive,
                user.EmailConfirmed,
                user.IsDealer,
                Roles = userRoles.ToArray()
            },
            expiresAt = DateTime.UtcNow.AddHours(8)
        });
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

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDto dto)
    {
        var (success, message) = await _userService.VerifyEmailAsync(dto);
        if (!success)
            return BadRequest(new { message });
        return Ok(new { message });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        var (success, message) = await _userService.ResetPasswordAsync(dto.EmailOrPhone, dto.Code, dto.NewPassword);
        if (!success)
            return BadRequest(new { message });
        return Ok(new { message });
    }
} 