using Microsoft.AspNetCore.Mvc;
using QR_Menu.Application.Common;
using QR_Menu.Application.Categories;
using QR_Menu.Application.Categories.DTOs;
using QR_Menu.Domain.Common;
using QR_Menu.Infrastructure.Authorization;
using System.Security.Claims;
using QR_Menu.Application.Admin;

namespace QR_Menu.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : BaseController
{
    private readonly CategoriesService _categoriesService;
    private readonly AdminService _adminService;

    public CategoriesController(CategoriesService categoriesService, AdminService adminService)
    {
        _categoriesService = categoriesService;
        _adminService = adminService;
    }

    [HttpGet("GetCategoriesByRestaurantId")]
    [RequirePermission(Permissions.Menu.ViewOwn)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<object>> GetByRestaurant(
        [FromQuery] Guid restaurantId,
        [FromQuery] string? search = null,
        [FromQuery] bool? active = null,
        [FromQuery] int? pageNumber = null,
        [FromQuery] int? pageSize = null)
    {
        // Authorization similar to RestaurantsController
        var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var isManager = roles.Contains(Roles.Manager);
        if (!isManager)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (userIdStr == null || !Guid.TryParse(userIdStr, out var currentUserId))
                return Unauthorized("Geçersiz kullanıcı", "Invalid user");

            var restaurant = await _adminService.GetRestaurantDetailAsync(restaurantId);
            if (restaurant == null) return NotFound("Restoran bulunamadı", "Restaurant not found");

            var isOwnerOfRestaurant = restaurant.UserId == currentUserId;
            var isDealerOfRestaurant = restaurant.DealerId.HasValue && restaurant.DealerId.Value == currentUserId;
            if (!isOwnerOfRestaurant && !isDealerOfRestaurant)
                return Forbid();
        }

        return await GetPaginatedDataAsync(
            async (page, size) => await _categoriesService.GetByRestaurantAsync(restaurantId, search, active, page, size),
            pageNumber,
            pageSize,
            "Kategoriler başarıyla getirildi",
            "Categories retrieved successfully");
    }

    [HttpGet("GetCategoryById")]
    [RequirePermission(Permissions.Menu.View)]
    public async Task<ActionResult<ResponsBase>> GetById([FromQuery] Guid id)
    {
        var category = await _categoriesService.GetByIdAsync(id);
        if (category == null) return NotFound("Kategori bulunamadı", "Category not found");
        return Success(category, "Kategori başarıyla getirildi", "Category retrieved successfully");
    }

    [HttpPost("AddCategory")]
    [RequirePermission(Permissions.Menu.Create)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResponsBase>> Create([FromBody] CategoryCreateDto dto)
    {
        // Authorization: Managers can create categories for any restaurant. Owners/Dealers only their own.
        var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var isManager = roles.Contains(Roles.Manager);
        if (!isManager)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (userIdStr == null || !Guid.TryParse(userIdStr, out var currentUserId))
                return Unauthorized("Geçersiz kullanıcı", "Invalid user");

            var restaurant = await _adminService.GetRestaurantDetailAsync(dto.RestaurantId);
            if (restaurant == null) 
                return NotFound("Restoran bulunamadı", "Restaurant not found");

            var isOwnerOfRestaurant = restaurant.UserId == currentUserId;
            var isDealerOfRestaurant = restaurant.DealerId.HasValue && restaurant.DealerId.Value == currentUserId;
            if (!isOwnerOfRestaurant && !isDealerOfRestaurant)
                return Forbid();
        }

        var (category, error) = await _categoriesService.CreateAsync(dto);
        if (category == null) 
            return BadRequest(error ?? "Kategori oluşturulamadı", "Category could not be created");
        
        return Success(category, "Kategori başarıyla oluşturuldu", "Category created successfully");
    }

    [HttpPut("UpdateCategory")]
    [RequirePermission(Permissions.Menu.Update)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResponsBase>> Update([FromQuery] Guid id, [FromBody] CategoryUpdateDto dto)
    {
        // First get the category to check restaurant ownership
        var category = await _categoriesService.GetByIdAsync(id);
        if (category == null) 
            return NotFound("Kategori bulunamadı", "Category not found");

        // Authorization: Managers can update categories for any restaurant. Owners/Dealers only their own.
        var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var isManager = roles.Contains(Roles.Manager);
        if (!isManager)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (userIdStr == null || !Guid.TryParse(userIdStr, out var currentUserId))
                return Unauthorized("Geçersiz kullanıcı", "Invalid user");

            var restaurant = await _adminService.GetRestaurantDetailAsync(category.RestaurantId);
            if (restaurant == null) 
                return NotFound("Restoran bulunamadı", "Restaurant not found");

            var isOwnerOfRestaurant = restaurant.UserId == currentUserId;
            var isDealerOfRestaurant = restaurant.DealerId.HasValue && restaurant.DealerId.Value == currentUserId;
            if (!isOwnerOfRestaurant && !isDealerOfRestaurant)
                return Forbid();
        }

        var ok = await _categoriesService.UpdateAsync(id, dto);
        if (!ok) 
            return NotFound("Kategori bulunamadı", "Category not found");
        
        // Return updated category
        var updatedCategory = await _categoriesService.GetByIdAsync(id);
        return Success(updatedCategory, "Kategori başarıyla güncellendi", "Category updated successfully");
    }

    [HttpDelete("DeleteCategory")]
    [RequirePermission(Permissions.Menu.Delete)]
    public async Task<ActionResult<ResponsBase>> Delete([FromQuery] Guid id)
    {
        var ok = await _categoriesService.DeleteAsync(id);
        if (!ok) return BadRequest("Kategori silinemedi. Ürün içeriyor olabilir.", "Category cannot be deleted. It may contain products.");
        return Success("Kategori başarıyla silindi", "Category deleted successfully");
    }
} 