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
using QR_Menu.Application.Common;
using System.Security.Claims;
using System.Net;

namespace QR_Menu.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RestaurantsController : ControllerBase
{
    private readonly RestaurantService _restaurantService;
    private readonly AdminService _adminService;
    private readonly LicenseService _licenseService;

    public RestaurantsController(RestaurantService restaurantService, AdminService adminService, LicenseService licenseService)
    {
        _restaurantService = restaurantService;
        _adminService = adminService;
        _licenseService = licenseService;
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
            return responsBase.StatusCode == "404" ? NotFound(responsBase) : Ok(responsBase);
        }
        
        // If response is data object (when pagination parameters are provided), return it directly
        return Ok(response);
    }

    /// <summary>
    /// Basic/Public view with essential info only
    /// </summary>
    [HttpGet("GetAllRestaurants-WithLessDetails")]
    [RequirePermission(Permissions.Restaurants.View)]
    public async Task<ActionResult<ResponsBase>> GetRestaurantsList(
        [FromQuery] string? search, 
        [FromQuery] string? city, 
        [FromQuery] bool? isActive, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20)
    {
        var (restaurants, total) = await _restaurantService.GetAllAsync(search, city, isActive, page, pageSize);
        var data = new { total, restaurants };
        return Ok(ResponsBase.Create("Restoran listesi başarıyla alındı", "Restaurant list retrieved successfully", "200", data));
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
            return Unauthorized(ResponsBase.Create("Geçersiz kullanıcı", "Invalid user", "401"));

        var (restaurants, total) = await _adminService.GetRestaurantsAsync(
            search, null, null, null, userId, null, null, null, page, pageSize);
        var data = new { total, restaurants };
        return Ok(ResponsBase.Create("Restoranlarınız başarıyla alındı", "Your restaurants retrieved successfully", "200", data));
    }

    /// <summary>
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
    }

    [HttpGet("GetRestaurantById")]
    [RequirePermission(Permissions.Restaurants.View)]
    public async Task<ActionResult<ResponsBase>> GetRestaurantById(Guid restaurantId)
    {
        var restaurant = await _adminService.GetRestaurantDetailAsync(restaurantId);
        if (restaurant == null) return NotFound(ResponsBase.Create("Restoran bulunamadı", "Restaurant not found", "404"));
        return Ok(ResponsBase.Create("Restoran detayları başarıyla alındı", "Restaurant details retrieved successfully", "200", restaurant));
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
            dataProvider: async (page, size) => await _adminService.GetRestaurantsByUserIdAsync(
                userId, page, size, searchKey, city, district, neighbourhood, active),
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
            return responsBase.StatusCode == "404" ? NotFound(responsBase) : Ok(responsBase);
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
        if (restaurant == null) return NotFound(ResponsBase.Create("Restoran bulunamadı", "Restaurant not found", "404"));
        return Ok(ResponsBase.Create("Restoran bilgileri başarıyla alındı", "Restaurant information retrieved successfully", "200", restaurant));
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
                return Unauthorized(ResponsBase.Create("Geçersiz kullanıcı", "Invalid user", "401"));
            userId = authenticatedUserId;
        }

        var (restaurant, errorMessage) = await _restaurantService.CreateAsync(dto, userId.Value);
        
        if (restaurant == null)
        {
            if (errorMessage == "Kullanıcı bulunamadı.")
                return NotFound(ResponsBase.Create("Kullanıcı bulunamadı.", "User not found.", "404"));
            else if (errorMessage == "Bayi bulunamadı.")
                return NotFound(ResponsBase.Create("Bayi bulunamadı.", "Dealer not found.", "404"));
            else
                return BadRequest(ResponsBase.Create(errorMessage ?? "Geçersiz istek", "Invalid request", "400"));
        }
        
        return Ok(ResponsBase.Create("Restoran başarıyla oluşturuldu", "Restaurant created successfully", "201", restaurant));
    }


    [HttpPut("UpdateRestaurantById")]
    [RequirePermission(Permissions.Restaurants.Update)]
    public async Task<ActionResult<ResponsBase>> UpdateRestaurant(Guid id, [FromBody] RestaurantUpdateDto dto)
    {
        var success = await _restaurantService.UpdateAsync(id, dto);
        if (!success) return NotFound(ResponsBase.Create("Restoran bulunamadı", "Restaurant not found", "404"));
        return Ok(ResponsBase.Create("Restoran başarıyla güncellendi", "Restaurant updated successfully", "200"));
    }

    [HttpPut("UpdateMyRestaurantById")]
    [RequirePermission(Permissions.Restaurants.UpdateOwn)]
    public async Task<ActionResult<ResponsBase>> UpdateMyRestaurant(Guid id, [FromBody] RestaurantUpdateDto dto)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (userIdStr == null || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized(ResponsBase.Create("Geçersiz kullanıcı", "Invalid user", "401"));

        var success = await _restaurantService.UpdateAsync(id, dto);
        if (!success) return NotFound(ResponsBase.Create("Restoran bulunamadı", "Restaurant not found", "404"));
        return Ok(ResponsBase.Create("Restoranınız başarıyla güncellendi", "Your restaurant updated successfully", "200"));
    }

    [HttpDelete("DeleteRestaurantById")]
    [RequirePermission(Permissions.Restaurants.Delete)]
    public async Task<ActionResult<ResponsBase>> DeleteRestaurant(Guid id)
    {
        var success = await _restaurantService.DeleteAsync(id);
        if (!success) return NotFound(ResponsBase.Create("Restoran bulunamadı", "Restaurant not found", "404"));
        return Ok(ResponsBase.Create("Restoran başarıyla silindi", "Restaurant deleted successfully", "200"));
    }

    [HttpGet("GetRestaurantLicensesById")]
    [RequirePermission(Permissions.Licenses.View)]
    public async Task<ActionResult<ResponsBase>> GetRestaurantLicenses(Guid id)
    {
        var licenses = await _licenseService.GetRestaurantLicensesAsync(id);
        return Ok(ResponsBase.Create("Restoran lisansları başarıyla alındı", "Restaurant licenses retrieved successfully", "200", licenses));
    }

    [HttpGet("GetDistinctCities")]
    [RequirePermission(Permissions.Restaurants.View)]
    public async Task<ActionResult<ResponsBase>> GetDistinctCities()
    {
        var cities = await _adminService.GetDistinctCitiesAsync();
        return Ok(ResponsBase.Create("Şehirler başarıyla alındı", "Cities retrieved successfully", "200", cities));
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
        return Ok(ResponsBase.Create("Toplu güncelleme tamamlandı", "Bulk update completed", "200", data));
    }
} 