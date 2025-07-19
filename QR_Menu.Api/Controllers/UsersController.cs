using Microsoft.AspNetCore.Mvc;
using QR_Menu.Application.Users;
using Microsoft.AspNetCore.Authorization;
using QR_Menu.Application.Users.DTOs;
using QR_Menu.Infrastructure.Authorization;
using QR_Menu.Domain.Common;
using QR_Menu.Application.Admin;
using QR_Menu.Application.Admin.DTOs;
using System.Security.Claims;
using System.Net;

namespace QR_Menu.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;
    private readonly AdminService _adminService;

    public UsersController(UserService userService, AdminService adminService)
    {
        _userService = userService;
        _adminService = adminService;
    }

    [HttpGet("GetAllUsers")]
    [RequirePermission(Permissions.Users.ViewAll)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<object>> GetUsers(
        [FromQuery] string? search,
        [FromQuery] string? role,
        [FromQuery] bool? isActive,
        [FromQuery] bool? emailConfirmed,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var (users, total) = await _adminService.GetUsersAsync(search, role, isActive, emailConfirmed, page, pageSize);
        return Ok(new { 
            total, 
            users, 
            page, 
            pageSize, 
            totalPages = (int)Math.Ceiling((double)total / pageSize) 
        });
    }

    [HttpGet("GetAllUsers-WithLessDetails")]
    [RequirePermission(Permissions.Users.View)]
    public async Task<ActionResult<object>> GetUsersList([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var (users, total) = await _userService.GetAllAsync(search, page, pageSize);
        return Ok(new { total, users });
    }

/// <summary>
/// it returns a user with all the details including restorants and his licenses
/// </summary>
    [HttpGet("GetUserDetailById")]
    [RequirePermission(Permissions.Users.ViewDetails)]
    public async Task<ActionResult<AdminUserDetailDto>> GetUserDetailById(Guid id)
    {
        var user = await _adminService.GetUserDetailAsync(id);
        if (user == null) return NotFound(new { message = "User not found" });
        return Ok(user);
    }

    [HttpGet("GetUserById")]
    [RequirePermission(Permissions.Users.View)]
    public async Task<ActionResult<UserReadDto>> GetUserBasic(Guid id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null) return NotFound();
        return Ok(user);
    }

    [HttpPost("CreateUser")]
    [RequirePermission(Permissions.Users.Create)]
    public async Task<ActionResult<UserReadDto>> CreateUser([FromBody] UserCreateDto dto)
    {
        var user = await _userService.CreateAsync(dto);
        if (user == null) return BadRequest(new { message = "Email already exists or validation failed." });
        return CreatedAtAction(nameof(GetUserBasic), new { id = user.Id }, user);
    }

    [HttpPut("UpdateUserById")]
    [RequirePermission(Permissions.Users.Update)]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UserUpdateDto dto)
    {
        var success = await _userService.UpdateAsync(id, dto);
        if (!success) return NotFound(new { message = "User not found" });
        return NoContent();
    }

    [HttpDelete("DeleteUserById")]
    [RequirePermission(Permissions.Users.Delete)]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var success = await _userService.DeleteAsync(id);
        if (!success) return NotFound(new { message = "User not found" });
        return NoContent();
    }

    [HttpGet("GetUserRestaurantsById")]
    [RequirePermission(Permissions.Restaurants.View)]
    public async Task<ActionResult<List<AdminRestaurantSummaryDto>>> GetUserRestaurants(Guid id)
    {
        var user = await _adminService.GetUserDetailAsync(id);
        if (user == null) return NotFound(new { message = "User not found" });
        return Ok(user.Restaurants);
    }

    [HttpGet("GetUserLicensesById")]
    [RequirePermission(Permissions.Licenses.View)]
    public async Task<ActionResult<List<AdminLicenseSummaryDto>>> GetUserLicenses(Guid id)
    {
        var user = await _adminService.GetUserDetailAsync(id);
        if (user == null) return NotFound(new { message = "User not found" });
        return Ok(user.Licenses);
    }

    [HttpGet("GetAvailableOwners")]
    [RequirePermission(Permissions.Users.View)]
    public async Task<ActionResult<List<AdminUserSummaryDto>>> GetAvailableOwners()
    {
        var owners = await _adminService.GetAvailableOwnersAsync();
        return Ok(owners);
    }

    [HttpGet("GetAvailableDealers")]
    [RequirePermission(Permissions.Users.View)]
    public async Task<ActionResult<List<AdminUserSummaryDto>>> GetAvailableDealers()
    {
        var dealers = await _adminService.GetAvailableDealersAsync();
        return Ok(dealers);
    }

    [HttpPut("bulk-status")]
    [RequirePermission(Permissions.Users.BulkOperations)]
    public async Task<IActionResult> BulkUpdateUserStatus([FromBody] BulkStatusUpdateDto dto)
    {
        var successCount = 0;
        foreach (var userId in dto.Ids)
        {
            var updateDto = new UserUpdateDto { IsActive = dto.IsActive };
            var success = await _userService.UpdateAsync(userId, updateDto);
            if (success) successCount++;
        }

        return Ok(new { 
            message = $"{successCount} users updated successfully", 
            successCount, 
            totalRequested = dto.Ids.Count 
        });
    }

    [HttpGet("GetProfile")]
    [Authorize]
    public async Task<ActionResult<UserReadDto>> GetProfile()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (userIdStr == null || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized(new { message = "Invalid user." });
            
        var user = await _userService.GetByIdAsync(userId);
        if (user == null) return NotFound();
        return Ok(user);
    }
}