using Microsoft.AspNetCore.Mvc;
using RestaurantSystem.Application.Users;
using RestaurantSystem.Application.Users.DTOs;

namespace RestaurantSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService)
    {
        _userService = userService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
    {
        var (success, message) = await _userService.RegisterAsync(dto);
        if (!success)
        {
            return BadRequest(new { message });
        }
        return Ok(new { message });
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDto dto)
    {
        var (success, message) = await _userService.VerifyEmailAsync(dto);
        if (!success)
        {
            return BadRequest(new { message });
        }
        return Ok(new { message });
    }
} 