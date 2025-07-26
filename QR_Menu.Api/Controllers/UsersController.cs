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

   /*  [HttpGet("GetUsers-WithLessDetails")]
    [RequirePermission(Permissions.Users.View)]
    public async Task<ActionResult<ResponsBase>> GetUsersList([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var (users, total) = await _userService.GetAllAsync(search, page, pageSize);
        var data = new { total, users };
        return Ok(ResponsBase.Create("Kullanıcı listesi başarıyla alındı", "User list retrieved successfully", "200", data));
    }*/


    [HttpGet("GetUserById")]
    [RequirePermission(Permissions.Users.ViewDetails)]
    public async Task<ActionResult<ResponsBase>> GetUserDetailById(Guid userId)
    {
        var data = await _adminService.GetUserDetailAsync(userId);
        if (data == null) return NotFound("Kullanıcı bulunamadı", "User not found");
        return Success(data, "Kullanıcı detayları başarıyla alındı", "User details retrieved successfully");
    }
/// <summary>
/// it returns a user with less details
/// </summary>
    [HttpGet("GetUserDetailById")]
    [RequirePermission(Permissions.Users.View)]
    public async Task<ActionResult<ResponsBase>> GetUserBasic(Guid id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null) return NotFound("Kullanıcı bulunamadı", "User not found");
        return Success(user, "Kullanıcı bilgileri başarıyla alındı", "User information retrieved successfully");
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
                return NotFound("Bayi bulunamadı.", "Dealer not found.");
            else
                return BadRequest(errorMessage ?? "E-posta zaten mevcut veya doğrulama başarısız", "Email already exists or validation failed");
        }
        
        return Success(user, "Kullanıcı başarıyla oluşturuldu", "User created successfully");
    }

    [HttpPut("UpdateUserById")]
    [RequirePermission(Permissions.Users.Update)]
    public async Task<ActionResult<ResponsBase>> UpdateUser([FromQuery] Guid id, [FromBody] UserUpdateDto dto)
    {
        // General user update endpoint - does NOT handle isActive status updates
        // Usage: PUT /api/Users/UpdateUserById?id={userId}
        // Body: { "firstName": "John", "lastName": "Doe", "email": "john@example.com", ... }
        
        var success = await _userService.UpdateAsync(id, dto);
        if (!success) return NotFound("Kullanıcı bulunamadı", "User not found");
        return Success("Kullanıcı başarıyla güncellendi", "User updated successfully");
    }

    [HttpPut("UpdateUserIsActive")]
    [RequirePermission(Permissions.Users.Update)]
    public async Task<ActionResult<ResponsBase>> UpdateUserIsActive(Guid userId, bool isActive, string? passiveNote = null)
    {
        var (success, errorMessage) = await _userService.UpdateUserIsActiveAsync(userId, isActive, passiveNote);
        
        if (!success)
        {
            if (errorMessage == "Kullanıcı bulunamadı.")
                return NotFound("Kullanıcı bulunamadı.", "User not found.");
            else
                return BadRequest(errorMessage ?? "Kullanıcı güncellenirken hata oluştu.", "Error occurred while updating user.");
        }

        return Success("Kullanıcı güncellendi.", "User updated.");
    }

    [HttpPut("UpdateUserIsVerify")]
    [RequirePermission(Permissions.Users.Update)]
    public async Task<ActionResult<ResponsBase>> UpdateUserIsVerified(Guid UserId, bool emailConfirmed)
    {
        var (success, errorMessage) = await _userService.UpdateUserIsVerifiedAsync(UserId, emailConfirmed );
        if(!success)
        {
            if (errorMessage == "Kullanıcı bulunamadı.")
                return NotFound("Kullanıcı bulunamadı.", "User not found.");

            else 
            {
                return BadRequest(errorMessage ?? "Kullanıcı güncellenirken hata oluştu.", "Error occurred while updating user.");
            }    
        }

        return Success("Kullanıcı güncellendi.", "User updated.");
    }

    [HttpPut("UpdateUserIsDealer")]
    [RequirePermission(Permissions.Users.Update)]
    public async Task<ActionResult<ResponsBase>> UpdateUserIsDealer(
        Guid userId, 
        bool isDealer, 
        bool sendSMSNotify = false, 
        bool sendEmailNotify = false)
    {
        var (success, errorMessage) = await _userService.UpdateUserIsDealerAsync(userId, isDealer, sendSMSNotify, sendEmailNotify);
        
        if (!success)
        {
            if (errorMessage == "Kullanıcı bulunamadı.")
                return NotFound("Kullanıcı bulunamadı.", "User not found.");
            else
                return BadRequest(errorMessage ?? "Kullanıcı güncellenirken hata oluştu.", "Error occurred while updating user.");
        }

        return Success("Kullanıcı güncellendi.", "User updated.");
    }

    [HttpPut("DealerTransfer")]
    [RequirePermission(Permissions.Users.Update)]
    public async Task<ActionResult<ResponsBase>> DealerTransfer(Guid userId, Guid dealerUserId)
    {
        var (success, errorMessage) = await _userService.DealerTransferAsync(userId, dealerUserId);
        
        if (!success)
        {
            if (errorMessage == "Kullanıcı bulunamadı.")
                return NotFound("Kullanıcı bulunamadı.", "User not found.");
            else if (errorMessage == "Bayi bulunamadı.")
                return NotFound("Bayi bulunamadı.", "Dealer not found.");
            else if (errorMessage == "Restoran bulunamadı.")
                return NotFound("Restoran bulunamadı.", "Restaurant not found.");
            else
                return BadRequest(errorMessage ?? "Transfer işlemi sırasında hata oluştu.", "Error occurred during transfer.");
        }

        return Success("Kullanıcı transfer edildi.", "User has been transferred.");
    }


    [HttpDelete("DeleteUserById")]
    [RequirePermission(Permissions.Users.Delete)]
    public async Task<ActionResult<ResponsBase>> DeleteUser(Guid userId)
    {
        var (success, errorMessage) = await _userService.DeleteUserByIdAsync(userId);
        
        if (!success)
        {
            if (errorMessage == "Kullanıcı bulunamadı.")
                return NotFound("Kullanıcı bulunamadı.", "User not found.");
            else if (errorMessage.Contains("restoranı var"))
                return BadRequest(errorMessage, "User has restaurants and cannot be deleted.");
            else if (errorMessage.Contains("lisansı var"))
                return BadRequest(errorMessage, "User has licenses and cannot be deleted.");
            else
                return BadRequest(errorMessage ?? "Kullanıcı silinirken hata oluştu.", "Error occurred while deleting user.");
        }
        
        return Success("Kullanıcı başarıyla silindi", "User deleted successfully");
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
        return Success(data, "Toplu güncelleme tamamlandı", "Bulk update completed");
    }

     

    [HttpGet("GetProfile")]
    [Authorize]
    public async Task<ActionResult<ResponsBase>> GetProfile()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (userIdStr == null || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized("Geçersiz kullanıcı", "Invalid user");
        var user = await _userService.GetByIdAsync(userId);
        if (user == null) return NotFound("Kullanıcı bulunamadı", "User not found");
        return Success(user, "Profil bilgileri başarıyla alındı", "Profile information retrieved successfully");
    }
}