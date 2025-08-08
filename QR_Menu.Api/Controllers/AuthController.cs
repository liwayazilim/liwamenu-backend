using Microsoft.AspNetCore.Mvc;
using QR_Menu.Application.Users;
using QR_Menu.Application.Users.DTOs;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using QR_Menu.Application.Common;
using QR_Menu.Domain.Common;
using QR_Menu.Domain;

namespace QR_Menu.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : BaseController
{
    private readonly UserService _userService;
    private readonly IConfiguration _config;
    private readonly UserManager<User> _userManager;

    public AuthController(UserService userService, IConfiguration config , UserManager<User> userManager)
    {
        _userService = userService;
        _config = config;
        _userManager = userManager;
    }

    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResponsBase>> Register([FromBody] RegisterUserDto dto)
    {
        var (success, message) = await _userService.RegisterAsync(dto);
        if (!success) return BadRequest(ResponsBase.Create(message, message, "400"));
        return Ok(ResponsBase.Create("Kayıt başarılı. Lütfen e-postanızı kontrol ederek hesabınızı doğrulayın.", "Registration successful. Please check your email to verify your account.", "200"));
    }

    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginUserDto dto)
    {
        var (success, message, user) = await _userService.LoginAsync(dto.EmailOrPhone, dto.Password);
        if (!success || user == null)
            return BadRequest(new { message });

        
        var identityUser = await _userManager.FindByIdAsync(user.Id.ToString());
        var userRoles = await _userManager.GetRolesAsync(identityUser!);

        // Determine role flags
        var isManager = userRoles.Contains(Roles.Manager) || user.Role == UserRole.Manager;
        var isOwner = userRoles.Contains(Roles.Owner) || user.Role == UserRole.Owner;

        // Generate JWT using Identity's token providers
        var jwt = await GenerateJwtTokenAsync(user, userRoles);
        
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
                IsTemporary = user.IsTemporary,
                IsManager = isManager,
                IsOwner = isOwner,
                Roles = userRoles.ToArray()
            },
            redirectTo = isManager ? "/admin" : "/dashboard",
            expiresAt = DateTime.UtcNow.AddHours(8)
        });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult<ResponsBase>> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (userIdStr == null || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized(ResponsBase.Create("Geçersiz kullanıcı", "Invalid user", "401"));

        var (success, message) = await _userService.ChangePasswordAsync(userId, dto.CurrentPassword, dto.NewPassword);
        if (!success) return BadRequest(ResponsBase.Create(message, message, "400"));
        return Ok(ResponsBase.Create("Şifre başarıyla değiştirildi", "Password changed successfully", "200"));
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<ActionResult<ResponsBase>> VerifyEmail([FromBody] VerifyEmailDto dto)
    {
        var (success, message) = await _userService.VerifyEmailAsync(dto);
        if (!success) return BadRequest(ResponsBase.Create(message, message, "400"));
        return Ok(ResponsBase.Create("E-posta başarıyla doğrulandı", "Email verified successfully", "200"));
    }

    private async Task<string> GenerateJwtTokenAsync(User user, IList<string> userRoles)
    {
        // Generate JWT with comprehensive claims using Identity's token providers
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
            new Claim("isDealer", user.IsDealer.ToString().ToLower()),
            new Claim("isTemporary", user.IsTemporary.ToString().ToLower()),
            new Claim("isManager", (user.Role == UserRole.Manager).ToString().ToLower()),
            new Claim("isOwner", (user.Role == UserRole.Owner).ToString().ToLower())
        };

        // Add all user roles to claims
        foreach (var role in userRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Add legacy role for backward compatibility
        claims.Add(new Claim("legacyRole", user.Role.ToString()));

        // Use Identity's token providers for better security
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
} 