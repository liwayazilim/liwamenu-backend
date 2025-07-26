using Microsoft.AspNetCore.Mvc;
using QR_Menu.Application.Restaurants;
using QR_Menu.Application.Restaurants.DTOs;
using Microsoft.AspNetCore.Authorization;
using QR_Menu.Infrastructure.Authorization;
using QR_Menu.Domain.Common;
using QR_Menu.Application.Admin;
using QR_Menu.Application.Admin.DTOs;
using QR_Menu.Application.Users.DTOs;
using QR_Menu.Application.Common;
using System.Security.Claims;
using System.Net;

namespace QR_Menu.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RestaurantsController : BaseController
{
    private readonly RestaurantService _restaurantService;
    private readonly AdminService _adminService;

    public RestaurantsController(RestaurantService restaurantService, AdminService adminService)
    {
        _restaurantService = restaurantService;
        _adminService = adminService;
    }

    /// <summary>
    /// Admin/Management view with full details
    /// </summary>
    [HttpGet("GetRestaurants")]
    [RequirePermission(Permissions.Restaurants.ViewAll)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<object>> GetAllRestaurants(
        [FromQuery] string? searchKey,
        [FromQuery] string? city,
        [FromQuery] bool? active,
        [FromQuery] bool? hasLicense,
        [FromQuery] Guid? ownerId,
        [FromQuery] Guid? dealerId,
        [FromQuery] string? district,
        [FromQuery] string? neighbourhood,
        [FromQuery] int? pageNumber = null,
        [FromQuery] int? pageSize = null)
    {
        var response = await PaginationHelper.CreatePaginatedResponseAsync(
            dataProvider: async (page, size) => await _adminService.GetRestaurantsAsync(
                searchKey, city, active, hasLicense, ownerId, dealerId, district, neighbourhood, page, size),
            pageNumber,
            pageSize,
            "Restoranlar başarıyla alındı",
            "Restaurants retrieved successfully",
            "Restoranlar bulunamadı",
            "Restaurants not found"
        );

        // If response is ResponsBase (when both parameters are null), handle it accordingly
        if (response is ResponsBase responsBase)
        {
            // Now PaginationHelper always returns 200 status, so we just return Ok
            return Ok(responsBase);
        }
        
        // If response is data object (when pagination parameters are provided), return it directly
        return Ok(response);
    }

    

    /// <summary>
    /// User must be logged in to see his own restoranlar
    /// </summary>
    [HttpGet("myRestaurants")]
    [RequirePermission(Permissions.Restaurants.ViewOwn)]
    public async Task<ActionResult<ResponsBase>> GetMyRestaurants(
        [FromQuery] string? search, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (userIdStr == null || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized("Geçersiz kullanıcı", "Invalid user");

        var (restaurants, total) = await _adminService.GetRestaurantsAsync(
            search, null, null, null, userId, null, null, null, page, pageSize);
        var data = new { total, restaurants };
        return Success(data, "Restoranlarınız başarıyla alındı", "Your restaurants retrieved successfully");
    }

    /* /// <summary>
    /// User must be logged in to see his licensed restoranlar
    /// </summary>
    [HttpGet("GetLicensedRestaurants")]
    [RequirePermission(Permissions.Restaurants.ViewLicensed)]
    public async Task<ActionResult<ResponsBase>> GetLicensedRestaurants(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (userIdStr == null || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized(ResponsBase.Create("Geçersiz kullanıcı", "Invalid user", "401"));

        var (restaurants, total) = await _adminService.GetRestaurantsAsync(
            search, null, null, null, null, userId, null, null, page, pageSize);
        var data = new { total, restaurants };
        return Ok(ResponsBase.Create("Lisanslı restoranlar başarıyla alındı", "Licensed restaurants retrieved successfully", "200", data));
    }*/

    [HttpGet("GetRestaurantById")]
    [RequirePermission(Permissions.Restaurants.View)]
    public async Task<ActionResult<ResponsBase>> GetRestaurantById(Guid restaurantId)
    {
        var restaurant = await _adminService.GetRestaurantDetailAsync(restaurantId);
        if (restaurant == null) return NotFound("Restoran bulunamadı", "Restaurant not found");
        return Success(restaurant, "Restoran detayları başarıyla alındı", "Restaurant details retrieved successfully");
    }

    [HttpGet("GetRestaurantsByUserId")]
    [RequirePermission(Permissions.Restaurants.ViewOwn)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<object>> GetRestaurantsByUserId(
        [FromQuery] Guid userId,
        [FromQuery] int? pageNumber = null,
        [FromQuery] int? pageSize = null,
        [FromQuery] string? searchKey = null,
        [FromQuery] string? city = null,
        [FromQuery] string? district = null,
        [FromQuery] string? neighbourhood = null,
        [FromQuery] bool? active = null)
        //[FromQuery] bool? hasLicense = null,
        //[FromQuery] bool? inPersonOrder = null,
        //[FromQuery] bool? onlineOrder = null,
    {
        var response = await PaginationHelper.CreatePaginatedResponseAsync(
            dataProvider: async (page, size) => await _adminService.GetRestaurantsAsync(
                searchKey, city, active, null, userId, null, district, neighbourhood, page, size),
            pageNumber,
            pageSize,
            "Kullanıcının restoranları başarıyla alındı",
            "User restaurants retrieved successfully",
            "Kullanıcının restoranı bulunamadı",
            "User restaurants not found"
        );

        // If response is ResponsBase (when both parameters are null), handle it accordingly
        if (response is ResponsBase responsBase)
        {
            // Now PaginationHelper always returns 200 status, so we just return Ok
            return Ok(responsBase);
        }
        
        // If response is data object (when pagination parameters are provided), return it directly
        return Ok(response);
    }

   

    /// <summary>
    /// Public restaurants can be viewed by anyone
    /// </summary>
    [HttpGet("GetRestaurantBasicById")] 
    [AllowAnonymous] 
    public async Task<ActionResult<ResponsBase>> GetRestaurantBasic(Guid id)
    {
        var restaurant = await _restaurantService.GetByIdAsync(id);
        if (restaurant == null) return NotFound("Restoran bulunamadı", "Restaurant not found");
        return Success(restaurant, "Restoran bilgileri başarıyla alındı", "Restaurant information retrieved successfully");
    }

    [HttpPost("AddRestaurant")]
    [RequirePermission(Permissions.Restaurants.Create)]
    public async Task<ActionResult<ResponsBase>> AddRestaurant(
        [FromQuery] Guid? userId,
        [FromBody] RestaurantCreateDto dto)
    {
        // If userId is not provided in query, get it from the authenticated user
        if (!userId.HasValue)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (userIdStr == null || !Guid.TryParse(userIdStr, out var authenticatedUserId))
                return Unauthorized("Geçersiz kullanıcı", "Invalid user");
            userId = authenticatedUserId;
        }

        var (restaurant, errorMessage) = await _restaurantService.CreateAsync(dto, userId.Value);
        
        if (restaurant == null)
        {
            if (errorMessage == "Kullanıcı bulunamadı.")
                return NotFound("Kullanıcı bulunamadı.", "User not found.");
            else if (errorMessage == "Bayi bulunamadı.")
                return NotFound("Bayi bulunamadı.", "Dealer not found.");
            else
                return BadRequest(errorMessage ?? "Geçersiz istek", "Invalid request");
        }
        
        return Success(restaurant, "Restoran başarıyla oluşturuldu", "Restaurant created successfully");
    }


    [HttpPut("UpdateRestaurantById")]
    [RequirePermission(Permissions.Restaurants.Update)]
    public async Task<ActionResult<ResponsBase>> UpdateRestaurant(Guid id, [FromBody] RestaurantUpdateDto dto)
    {
        var success = await _restaurantService.UpdateAsync(id, dto);
        if (!success) return NotFound("Restoran bulunamadı", "Restaurant not found");
        return Success("Restoran başarıyla güncellendi", "Restaurant updated successfully");
    }

    [HttpPut("UpdateMyRestaurantById")]
    [RequirePermission(Permissions.Restaurants.UpdateOwn)]
    public async Task<ActionResult<ResponsBase>> UpdateMyRestaurant(Guid id, [FromBody] RestaurantUpdateDto dto)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (userIdStr == null || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized("Geçersiz kullanıcı", "Invalid user");

        var success = await _restaurantService.UpdateAsync(id, dto);
        if (!success) return NotFound("Restoran bulunamadı", "Restaurant not found");
        return Success("Restoranınız başarıyla güncellendi", "Your restaurant updated successfully");
    }

    [HttpDelete("DeleteRestaurantById")]
    [RequirePermission(Permissions.Restaurants.Delete)]
    public async Task<ActionResult<ResponsBase>> DeleteRestaurant(Guid id)
    {
        var success = await _restaurantService.DeleteAsync(id);
        if (!success) return NotFound("Restoran bulunamadı", "Restaurant not found");
        return Success("Restoran başarıyla silindi", "Restaurant deleted successfully");
    }



    [HttpPut("bulk-status")]
    [RequirePermission(Permissions.Restaurants.BulkOperations)]
    public async Task<ActionResult<ResponsBase>> BulkUpdateRestaurantStatus([FromBody] BulkStatusUpdateDto dto)
    {
        var successCount = 0;
        foreach (var restaurantId in dto.Ids)
        {
            var updateDto = new RestaurantUpdateDto { IsActive = dto.IsActive ?? true };
            var success = await _restaurantService.UpdateAsync(restaurantId, updateDto);
            if (success) successCount++;
        }
        var data = new {
            message = $"{successCount} restoran başarıyla güncellendi",
            successCount,
            totalRequested = dto.Ids.Count
        };
        return Success(data, "Toplu güncelleme tamamlandı", "Bulk update completed");
    }
} 