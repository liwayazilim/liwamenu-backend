using Microsoft.AspNetCore.Mvc;
using QR_Menu.Application.Restaurants;
using QR_Menu.Application.Restaurants.DTOs;
using Microsoft.AspNetCore.Authorization;
using QR_Menu.Infrastructure.Authorization;
using QR_Menu.Domain.Common;
using QR_Menu.Application.Admin;
using QR_Menu.Application.Admin.DTOs;
using QR_Menu.Application.Licenses;
using QR_Menu.Application.Users.DTOs;
using System.Security.Claims;
using System.Net;

namespace QR_Menu.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RestaurantController : ControllerBase
{
    private readonly RestaurantService _restaurantService;
    private readonly AdminService _adminService;
    private readonly LicenseService _licenseService;

    public RestaurantController(RestaurantService restaurantService, AdminService adminService, LicenseService licenseService)
    {
        _restaurantService = restaurantService;
        _adminService = adminService;
        _licenseService = licenseService;
    }

    /// <summary>
    /// Admin/Management view with full details
    /// </summary>
    [HttpGet("GetAllRestaurants")]
    [RequirePermission(Permissions.Restaurants.ViewAll)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<object>> GetAllRestaurants(
        [FromQuery] string? search,
        [FromQuery] string? city,
        [FromQuery] bool? isActive,
        [FromQuery] bool? hasLicense,
        [FromQuery] Guid? ownerId,
        [FromQuery] Guid? dealerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var (restaurants, total) = await _adminService.GetRestaurantsAsync(
            search, city, isActive, hasLicense, ownerId, dealerId, page, pageSize);
        return Ok(new { 
            total, 
            restaurants, 
            page, 
            pageSize, 
            totalPages = (int)Math.Ceiling((double)total / pageSize) 
        });
    }
/// <summary>
/// Basic/Public view with essential info only
/// </summary>
    [HttpGet("GetAllRestaurants-WithLessDetails")]
    [RequirePermission(Permissions.Restaurants.View)]
    public async Task<ActionResult<object>> GetRestaurantsList(
        [FromQuery] string? search, 
        [FromQuery] string? city, 
        [FromQuery] bool? isActive, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20)
    {
        var (restaurants, total) = await _restaurantService.GetAllAsync(search, city, isActive, page, pageSize);
        return Ok(new { total, restaurants });
    }
/// <summary>
/// User must be logged in to see his own restoranlar
/// </summary>
    [HttpGet("myRestaurants")]
    [RequirePermission(Permissions.Restaurants.ViewOwn)]
    public async Task<ActionResult<object>> GetMyRestaurants(
        [FromQuery] string? search, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (userIdStr == null || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized(new { message = "Invalid user." });

        var (restaurants, total) = await _adminService.GetRestaurantsAsync(
            search, null, null, null, userId, null, page, pageSize);
        return Ok(new { total, restaurants });
    }

/// <summary>
/// User must be logged in to see his licensed restoranlar
/// </summary>
    [HttpGet("GetLicensedRestaurants")]
    [RequirePermission(Permissions.Restaurants.ViewLicensed)]
    public async Task<ActionResult<object>> GetLicensedRestaurants(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (userIdStr == null || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized(new { message = "Invalid user." });

        var (restaurants, total) = await _adminService.GetRestaurantsAsync(
            search, null, null, null, null, userId, page, pageSize);
        return Ok(new { total, restaurants });
    }

    [HttpGet("GetRestaurantDetailById")]
    [RequirePermission(Permissions.Restaurants.View)]
    public async Task<ActionResult<AdminRestaurantDetailDto>> GetRestaurantDetail(Guid id)
    {
        var restaurant = await _adminService.GetRestaurantDetailAsync(id);
        if (restaurant == null) return NotFound(new { message = "Restaurant not found" });
        return Ok(restaurant);
    }
/// <summary>
/// Public restaurants can be viewed by anyone
/// </summary>
    [HttpGet("GetRestaurantBasicById")] 
    [AllowAnonymous] 
    public async Task<ActionResult<RestaurantReadDto>> GetRestaurantBasic(Guid id)
    {
        var result = await _restaurantService.GetByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost("CreateRestaurant")]
    [RequirePermission(Permissions.Restaurants.Create)]
    public async Task<ActionResult<RestaurantReadDto>> CreateRestaurant([FromBody] RestaurantCreateDto dto)
    {
        var created = await _restaurantService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetRestaurantBasic), new { id = created.Id }, created);
    }

    [HttpPut("UpdateRestaurantById")]
    [RequirePermission(Permissions.Restaurants.Update)]
    public async Task<IActionResult> UpdateRestaurant(Guid id, [FromBody] RestaurantUpdateDto dto)
    {
        var success = await _restaurantService.UpdateAsync(id, dto);
        if (!success) return NotFound(new { message = "Restaurant not found" });
        return NoContent();
    }

    [HttpPut("UpdateMyRestaurantById")]
    [RequirePermission(Permissions.Restaurants.UpdateOwn)]
    public async Task<IActionResult> UpdateMyRestaurant(Guid id, [FromBody] RestaurantUpdateDto dto)
    {
        // TODO: Add ownership verification
        var success = await _restaurantService.UpdateAsync(id, dto);
        if (!success) return NotFound(new { message = "Restaurant not found" });
        return NoContent();
    }

    [HttpDelete("DeleteRestaurantById")]
    [RequirePermission(Permissions.Restaurants.Delete)]
    public async Task<IActionResult> DeleteRestaurant(Guid id)
    {
        var success = await _restaurantService.DeleteAsync(id);
        if (!success) return NotFound(new { message = "Restaurant not found" });
        return NoContent();
    }

    [HttpGet("GetRestaurantLicensesById")]
    [RequirePermission(Permissions.Licenses.View)]
    public async Task<ActionResult<List<AdminLicenseDto>>> GetRestaurantLicenses(Guid id)
    {
        var licenses = await _licenseService.GetRestaurantLicensesAsync(id);
        return Ok(licenses);
    }

    [HttpGet("GetDistinctCities")]
    [RequirePermission(Permissions.Restaurants.View)]
    public async Task<ActionResult<List<string>>> GetDistinctCities()
    {
        var cities = await _adminService.GetDistinctCitiesAsync();
        return Ok(cities);
    }
    
/// <summary>
/// (active/inactive) for multiple restaurants at once
/// </summary>
    [HttpPut("bulk-status")]
    [RequirePermission(Permissions.Restaurants.BulkOperations)]
    public async Task<IActionResult> BulkUpdateRestaurantStatus([FromBody] BulkStatusUpdateDto dto)
    {
        var successCount = 0;
        foreach (var restaurantId in dto.Ids)
        {
            var updateDto = new RestaurantUpdateDto { IsActive = dto.IsActive ?? true };
            var success = await _restaurantService.UpdateAsync(restaurantId, updateDto);
            if (success) successCount++;
        }

        return Ok(new { 
            message = $"{successCount} restaurants updated successfully", 
            successCount, 
            totalRequested = dto.Ids.Count 
        });
    }

   
} 