using Microsoft.AspNetCore.Mvc;
using QR_Menu.Application.Common;
using QR_Menu.Application.OrderTags;
using QR_Menu.Domain;
using QR_Menu.Domain.Common;
using QR_Menu.Infrastructure.Authorization;
using System.Security.Claims;
using QR_Menu.Application.Admin;

namespace QR_Menu.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderTagsController : BaseController
{
    private readonly OrderTagsService _orderTagsService;
    private readonly AdminService _adminService;

    public OrderTagsController(OrderTagsService orderTagsService, AdminService adminService)
    {
        _orderTagsService = orderTagsService;
        _adminService = adminService;
    }

    [HttpGet("GetOrderTagsByRestaurantId")]
    [RequirePermission(Permissions.Menu.ViewOwn)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<object>> GetByRestaurant(
        [FromQuery] Guid restaurantId,
        [FromQuery] string? search = null,
        [FromQuery] int? pageNumber = null,
        [FromQuery] int? pageSize = null)
    {
        // Authorization: Managers can access any restaurant. Owners/Dealers only their own.
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
            async (page, size) => await _orderTagsService.GetByRestaurantAsync(restaurantId, search, page, size),
            pageNumber,
            pageSize,
            "Sipariş etiketleri başarıyla getirildi",
            "Order tags retrieved successfully");
    }

    [HttpGet("GetOrderTagById")]
    [RequirePermission(Permissions.Menu.View)]
    public async Task<ActionResult<ResponsBase>> GetById([FromQuery] Guid id)
    {
        var tag = await _orderTagsService.GetByIdAsync(id);
        if (tag == null) return NotFound("Sipariş etiketi bulunamadı", "Order tag not found");
        return Success(tag, "Sipariş etiketi başarıyla getirildi", "Order tag retrieved successfully");
    }

    [HttpPost("BulkAddOrderTags")]
    [RequirePermission(Permissions.Menu.Create)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResponsBase>> BulkCreate([FromBody] BulkOrderTagCreateDto dto)
    {
        // Authorization: Managers can create tags for any restaurant. Owners/Dealers only their own.
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

        var (tags, error) = await _orderTagsService.BulkCreateAsync(dto);
        if (!tags.Any() && !string.IsNullOrEmpty(error))
            return BadRequest(error, "Sipariş etiketleri oluşturulamadı");
        
        var message = tags.Count == 1 
            ? "1 sipariş etiketi başarıyla oluşturuldu" 
            : $"{tags.Count} sipariş etiketi başarıyla oluşturuldu";
        
        var messageEN = tags.Count == 1 
            ? "1 order tag created successfully" 
            : $"{tags.Count} order tags created successfully";

        var data = new { tags, createdCount = tags.Count, error };
        return Success(data, message, messageEN);
    }

    [HttpPut("BulkUpdateOrderTags")]
    [RequirePermission(Permissions.Menu.Update)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResponsBase>> BulkUpdate([FromBody] BulkOrderTagUpdateDto dto)
    {
        // Authorization: Managers can update tags for any restaurant. Owners/Dealers only their own.
        var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var isManager = roles.Contains(Roles.Manager);
        if (!isManager)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (userIdStr == null || !Guid.TryParse(userIdStr, out var currentUserId))
                return Unauthorized("Geçersiz kullanıcı", "Invalid user");

            // Check if user has access to all tags being updated
            foreach (var tagDto in dto.Tags)
            {
                var tag = await _orderTagsService.GetByIdAsync(tagDto.Id);
                if (tag == null) continue;

                var restaurant = await _adminService.GetRestaurantDetailAsync(tag.RestaurantId);
                if (restaurant == null) continue;

                var isOwnerOfRestaurant = restaurant.UserId == currentUserId;
                var isDealerOfRestaurant = restaurant.DealerId.HasValue && restaurant.DealerId.Value == currentUserId;
                if (!isOwnerOfRestaurant && !isDealerOfRestaurant)
                    return Forbid();
            }
        }

        var (success, error) = await _orderTagsService.BulkUpdateAsync(dto);
        if (!success && !string.IsNullOrEmpty(error))
            return BadRequest(error, "Sipariş etiketleri güncellenemedi");
        
        var message = "Sipariş etiketleri toplu güncelleme tamamlandı";
        var messageEN = "Order tags bulk update completed";
        
        var data = new { success, error };
        return Success(data, message, messageEN);
    }

    [HttpDelete("DeleteOrderTag")]
    [RequirePermission(Permissions.Menu.Delete)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResponsBase>> Delete([FromQuery] Guid id)
    {
        // First get the tag to check restaurant ownership
        var tag = await _orderTagsService.GetByIdAsync(id);
        if (tag == null) 
            return NotFound("Sipariş etiketi bulunamadı", "Order tag not found");

        // Authorization: Managers can delete tags for any restaurant. Owners/Dealers only their own.
        var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var isManager = roles.Contains(Roles.Manager);
        if (!isManager)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (userIdStr == null || !Guid.TryParse(userIdStr, out var currentUserId))
                return Unauthorized("Geçersiz kullanıcı", "Invalid user");

            var restaurant = await _adminService.GetRestaurantDetailAsync(tag.RestaurantId);
            if (restaurant == null) 
                return NotFound("Restoran bulunamadı", "Restaurant not found");

            var isOwnerOfRestaurant = restaurant.UserId == currentUserId;
            var isDealerOfRestaurant = restaurant.DealerId.HasValue && restaurant.DealerId.Value == currentUserId;
            if (!isOwnerOfRestaurant && !isDealerOfRestaurant)
                return Forbid();
        }

        var success = await _orderTagsService.DeleteAsync(id);
        if (!success) 
            return BadRequest("Sipariş etiketi silinemez. Siparişlerde kullanılıyor olabilir.", "Order tag cannot be deleted. It may be in use by orders.");
        
        return Success("Sipariş etiketi başarıyla silindi", "Order tag deleted successfully");
    }
} 