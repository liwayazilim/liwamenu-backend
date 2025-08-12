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
    private readonly IWebHostEnvironment _environment;

    public RestaurantsController(RestaurantService restaurantService, AdminService adminService, IWebHostEnvironment environment)
    {
        _restaurantService = restaurantService;
        _adminService = adminService;
        _environment = environment;
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
        // to get the imge absolute path: 
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        
        var response = await PaginationHelper.CreatePaginatedResponseAsync(
            dataProvider: async (page, size) =>
            {
                var (restaurants, total) = await _adminService.GetRestaurantsAsync(
                    searchKey, city, active, hasLicense, ownerId, dealerId, district, neighbourhood, page, size, baseUrl);
                return (restaurants, total);
            },
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
    [HttpGet("GetmyRestaurants")]
    [RequirePermission(Permissions.Restaurants.ViewOwn)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<object>> GetMyRestaurants(
        [FromQuery] string? searchKey = null,
        [FromQuery] string? city = null,
        [FromQuery] string? district = null,
        [FromQuery] string? neighbourhood = null,
        [FromQuery] bool? active = null,
        [FromQuery] bool? hasLicense = null,
        [FromQuery] int? pageNumber = null,
        [FromQuery] int? pageSize = null)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (userIdStr == null || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized("Geçersiz kullanıcı", "Invalid user");

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var response = await PaginationHelper.CreatePaginatedResponseAsync(
            dataProvider: async (page, size) =>
            {
                var (restaurants, total) = await _adminService.GetRestaurantsAsync(
                    searchKey, city, active, hasLicense, userId, null, district, neighbourhood, page, size, baseUrl);
                return (restaurants, total);
            },
            pageNumber,
            pageSize,
            "Restoranlarınız başarıyla alındı",
            "Your restaurants retrieved successfully",
            "Restoranlarınız bulunamadı",
            "Your restaurants not found"
        );

        if (response is ResponsBase responsBase)
        {
            return Ok(responsBase);
        }

        return Ok(response);
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
[RequirePermission(Permissions.Restaurants.ViewOwn)]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public async Task<ActionResult<ResponsBase>> GetRestaurantById([FromQuery] Guid restaurantId)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var restaurant = await _adminService.GetRestaurantDetailAsync(restaurantId, baseUrl);
        if (restaurant == null) return NotFound("Restoran bulunamadı", "Restaurant not found");

        // Authorization: Managers can access any restaurant. Owners/Dealers only their own.
        var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var isManager = roles.Contains(Roles.Manager);
        if (!isManager)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (userIdStr == null || !Guid.TryParse(userIdStr, out var currentUserId))
                return Unauthorized("Geçersiz kullanıcı", "Invalid user");

            var isOwnerOfRestaurant = restaurant.UserId == currentUserId;
            var isDealerOfRestaurant = restaurant.DealerId.HasValue && restaurant.DealerId.Value == currentUserId;
            if (!isOwnerOfRestaurant && !isDealerOfRestaurant)
                return Forbid();
        }

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
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var response = await PaginationHelper.CreatePaginatedResponseAsync(
            dataProvider: async (page, size) =>
            {
                var (restaurants, total) = await _adminService.GetRestaurantsAsync(
                    searchKey, city, active, null, userId, null, district, neighbourhood, page, size, baseUrl);
                return (restaurants, total);
            },
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
        [FromForm] RestaurantCreateDto dto)
    {
        // If userId is not provided in query, get it from the authenticated user
        if (!userId.HasValue)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (userIdStr == null || !Guid.TryParse(userIdStr, out var authenticatedUserId))
                return Unauthorized("Geçersiz kullanıcı", "Invalid user");
            userId = authenticatedUserId;
        }

        var (restaurant, errorMessage) = await _restaurantService.CreateAsync(dto, userId.Value, _environment.WebRootPath);
        
        if (restaurant == null)
        {
            if (errorMessage == "Kullanıcı bulunamadı.")
                return NotFound("Kullanıcı bulunamadı.", "User not found.");
            else if (errorMessage == "Bayi bulunamadı.")
                return NotFound("Bayi bulunamadı.", "Dealer not found.");
            else if (errorMessage.Contains("Geçersiz resim"))
                return BadRequest(errorMessage, "Invalid image file");
            else
                return BadRequest(errorMessage ?? "Geçersiz istek", "Invalid request");
        }
        
        return Success(restaurant, "Restoran başarıyla oluşturuldu", "Restaurant created successfully");
    }


    [HttpPut("UpdateRestaurant")]
    [RequirePermission(Permissions.Restaurants.Update)]
    public async Task<ActionResult<ResponsBase>> UpdateRestaurant([FromForm] RestaurantUpdateDto dto)
    {
        var (restaurant, errorMessage) = await _restaurantService.UpdateAsync(dto.RestaurantId, dto, _environment.WebRootPath);
        
        if (restaurant == null)
        {
            if (errorMessage == "Restoran bulunamadı.")
                return NotFound("Restoran bulunamadı.", "Restaurant not found.");
            else if (errorMessage == "Geçersiz resim dosyası.")
                return BadRequest("Geçersiz resim dosyası.", "Invalid image file.");
            else if (errorMessage == "Resim işlenirken hata oluştu.")
                return BadRequest("Resim işlenirken hata oluştu.", "Error occurred while processing image.");
            else
                return BadRequest(errorMessage ?? "Restoran güncellenirken hata oluştu.", "Error occurred while updating restaurant.");
        }
        
        return Success(restaurant, "Restoran başarıyla güncellendi", "Restaurant updated successfully");
    }

   

    [HttpDelete("DeleteRestaurantById")]
    [RequirePermission(Permissions.Restaurants.Delete)]
    public async Task<ActionResult<ResponsBase>> DeleteRestaurant(Guid restaurantId)
    {
        var (success, errorMessage) = await _restaurantService.DeleteRestaurantByIdAsync(restaurantId, _environment.WebRootPath);
        
        if (!success)
        {
            if (errorMessage == "Restoran bulunamadı.")
                return NotFound("Restoran bulunamadı.", "Restaurant not found.");
            else if (errorMessage.Contains("lisansı var"))
                return BadRequest(errorMessage, "Restaurant has licenses and cannot be deleted.");
            else
                return BadRequest(errorMessage ?? "Restoran silinirken hata oluştu.", "Error occurred while deleting restaurant.");
        }
        
        return Success("Restoran başarıyla silindi", "Restaurant deleted successfully");
    }

    [HttpPut("RestaurantTransfer")]
    [RequirePermission(Permissions.Restaurants.ManageOwnership)]
    public async Task<ActionResult<ResponsBase>> RestaurantTransfer(Guid userId, Guid restaurantId)
    {
        var (success, errorMessage) = await _restaurantService.RestaurantTransferAsync(restaurantId, userId);
        
        if (!success)
        {
            if (errorMessage == "Restoran bulunamadı.")
                return NotFound("Restoran bulunamadı.", "Restaurant not found.");
            else if (errorMessage == "Kullanıcı bulunamadı.")
                return NotFound("Kullanıcı bulunamadı.", "User not found.");
            else if (errorMessage == "Lisanslar bulunamadı.")
                return NotFound("Lisanslar bulunamadı.", "Licenses not found.");
            else
                return BadRequest(errorMessage ?? "Restoran transfer edilirken hata oluştu.", "Error occurred while transferring restaurant.");
        }
        
        return Success("Restoran transfer edildi.", "Restaurant has been transferred.");
    }




} 