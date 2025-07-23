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
using QR_Menu.Application.Common;

namespace QR_Menu.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : BaseController
{
    private readonly UserService _userService;
    private readonly AdminService _adminService;

    public UsersController(UserService userService, AdminService adminService)
    {
        _userService = userService;
        _adminService = adminService;
    }

    [HttpGet("GetUsers")]
    [RequirePermission(Permissions.Users.ViewAll)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<object>> GetUsers(
        [FromQuery] string? searchKey,
        [FromQuery] string? role,
        [FromQuery] string? city,
        [FromQuery] bool? active,
        [FromQuery] bool? dealer,
        [FromQuery] bool? emailConfirmed,
        [FromQuery] int? pageNumber = null,
        [FromQuery] int? pageSize = null)
    {
        return await GetPaginatedDataAsync(
            dataProvider: async (page, size) => await _adminService.GetUsersAsync(
                searchKey, dealer, role,city, active, emailConfirmed, page, size),
            pageNumber,
            pageSize,
            "Kullanıcılar başarıyla alındı",
            "Users retrieved successfully",
            "Kullanıcılar bulunamadı",
            "Users not found"
        );
    }

    [HttpGet("GetUsers-WithLessDetails")]
    [RequirePermission(Permissions.Users.View)]
    public async Task<ActionResult<ResponsBase>> GetUsersList([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var (users, total) = await _userService.GetAllAsync(search, page, pageSize);
        var data = new { total, users };
        return Ok(ResponsBase.Create("Kullanıcı listesi başarıyla alındı", "User list retrieved successfully", "200", data));
    }


    [HttpGet("GetUserById")]
    [RequirePermission(Permissions.Users.ViewDetails)]
    public async Task<ActionResult<ResponsBase>> GetUserDetailById(Guid userId)
    {
        var data = await _adminService.GetUserDetailAsync(userId);
        if (data == null) return NotFound(ResponsBase.Create("Kullanıcı bulunamadı", "User not found", "404"));
        return Ok(ResponsBase.Create("Kullanıcı detayları başarıyla alındı", "User details retrieved successfully", "200", data));
    }
/// <summary>
/// it returns a user with less details
/// </summary>
    [HttpGet("GetUserDetailById")]
    [RequirePermission(Permissions.Users.View)]
    public async Task<ActionResult<ResponsBase>> GetUserBasic(Guid id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null) return NotFound(ResponsBase.Create("Kullanıcı bulunamadı", "User not found", "404"));
        return Ok(ResponsBase.Create("Kullanıcı bilgileri başarıyla alındı", "User information retrieved successfully", "200", user));
    }

    [HttpPost("AddUser")]
    [RequirePermission(Permissions.Users.Create)]
    public async Task<ActionResult<ResponsBase>> CreateUser([FromBody] UserCreateDto dto)
    {
        var (user, errorMessage) = await _userService.CreateAsync(dto);
        
        if (user == null)
        {
            if (errorMessage == "Kullanıcı zaten var.")
                return Conflict(ResponsBase.Create("Kullanıcı zaten var.", "User already exists.", "409"));
            else if (errorMessage == "Telefon numarası zaten kullanılıyor.")
                return Conflict(ResponsBase.Create("Telefon numarası zaten kullanılıyor.", "Phone number already exists.", "409"));
            else if (errorMessage == "Bayi bulunamadı.")
                return NotFound(ResponsBase.Create("Bayi bulunamadı.", "Dealer not found.", "404"));
            else
                return BadRequest(ResponsBase.Create(errorMessage ?? "E-posta zaten mevcut veya doğrulama başarısız", "Email already exists or validation failed", "400"));
        }
        
        return Ok(ResponsBase.Create("Kullanıcı başarıyla oluşturuldu", "User created successfully", "201", user));
    }

    [HttpPut("UpdateUserById")]
    [RequirePermission(Permissions.Users.Update)]
    public async Task<ActionResult<ResponsBase>> UpdateUser(Guid id, [FromBody] UserUpdateDto dto)
    {
        var success = await _userService.UpdateAsync(id, dto);
        if (!success) return NotFound(ResponsBase.Create("Kullanıcı bulunamadı", "User not found", "404"));
        return Ok(ResponsBase.Create("Kullanıcı başarıyla güncellendi", "User updated successfully", "200"));
    }

    [HttpDelete("DeleteUserById")]
    [RequirePermission(Permissions.Users.Delete)]
    public async Task<ActionResult<ResponsBase>> DeleteUser(Guid id)
    {
        var success = await _userService.DeleteAsync(id);
        if (!success) return NotFound(ResponsBase.Create("Kullanıcı bulunamadı", "User not found", "404"));
        return Ok(ResponsBase.Create("Kullanıcı başarıyla silindi", "User deleted successfully", "200"));
    }

    [HttpGet("GetUserRestaurantsById")]
    [RequirePermission(Permissions.Restaurants.View)]
    public async Task<ActionResult<ResponsBase>> GetUserRestaurants(Guid id)
    {
        var user = await _adminService.GetUserDetailAsync(id);
        if (user == null) return NotFound(ResponsBase.Create("Kullanıcı bulunamadı", "User not found", "404"));
        return Ok(ResponsBase.Create("Kullanıcı restoranları başarıyla alındı", "User restaurants retrieved successfully", "200", user.Restaurants));
    }

    [HttpGet("GetUserLicensesById")]
    [RequirePermission(Permissions.Licenses.View)]
    public async Task<ActionResult<ResponsBase>> GetUserLicenses(Guid id)
    {
        var user = await _adminService.GetUserDetailAsync(id);
        if (user == null) return NotFound(ResponsBase.Create("Kullanıcı bulunamadı", "User not found", "404"));
        return Ok(ResponsBase.Create("Kullanıcı lisansları başarıyla alındı", "User licenses retrieved successfully", "200", user.Licenses));
    }

    [HttpGet("GetAvailableOwners")]
    [RequirePermission(Permissions.Users.View)]
    public async Task<ActionResult<ResponsBase>> GetAvailableOwners()
    {
        var owners = await _adminService.GetAvailableOwnersAsync();
        return Ok(ResponsBase.Create("Mevcut sahipler başarıyla alındı", "Available owners retrieved successfully", "200", owners));
    }

    [HttpGet("GetAvailableDealers")]
    [RequirePermission(Permissions.Users.View)]
    public async Task<ActionResult<ResponsBase>> GetAvailableDealers()
    {
        var dealers = await _adminService.GetAvailableDealersAsync();
        return Ok(ResponsBase.Create("Mevcut bayiler başarıyla alındı", "Available dealers retrieved successfully", "200", dealers));
    }

    [HttpPut("bulk-status")]
    [RequirePermission(Permissions.Users.BulkOperations)]
    public async Task<ActionResult<ResponsBase>> BulkUpdateUserStatus([FromBody] BulkStatusUpdateDto dto)
    {
        var successCount = 0;
        foreach (var userId in dto.Ids)
        {
            var updateDto = new UserUpdateDto { IsActive = dto.IsActive };
            var success = await _userService.UpdateAsync(userId, updateDto);
            if (success) successCount++;
        }
        var data = new {
            message = $"{successCount} kullanıcı başarıyla güncellendi",
            successCount,
            totalRequested = dto.Ids.Count
        };
        return Ok(ResponsBase.Create("Toplu güncelleme tamamlandı", "Bulk update completed", "200", data));
    }

    [HttpGet("GetProfile")]
    [Authorize]
    public async Task<ActionResult<ResponsBase>> GetProfile()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (userIdStr == null || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized(ResponsBase.Create("Geçersiz kullanıcı", "Invalid user", "401"));
        var user = await _userService.GetByIdAsync(userId);
        if (user == null) return NotFound(ResponsBase.Create("Kullanıcı bulunamadı", "User not found", "404"));
        return Ok(ResponsBase.Create("Profil bilgileri başarıyla alındı", "Profile information retrieved successfully", "200", user));
    }
}