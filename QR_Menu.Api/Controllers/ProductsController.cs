using Microsoft.AspNetCore.Mvc;
using QR_Menu.Application.Common;
using QR_Menu.Application.Products;
using QR_Menu.Application.Products.DTOs;
using QR_Menu.Domain.Common;
using QR_Menu.Infrastructure.Authorization;
using System.Security.Claims;
using QR_Menu.Application.Admin;

namespace QR_Menu.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : BaseController
{
    private readonly ProductsService _productsService;
    private readonly AdminService _adminService;

    public ProductsController(ProductsService productsService, AdminService adminService)
    {
        _productsService = productsService;
        _adminService = adminService;
    }

    [HttpGet("GetProductsByRestaurantId")]
    [RequirePermission(Permissions.Menu.ViewOwn)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<object>> GetByRestaurant(
        [FromQuery] Guid restaurantId,
        [FromQuery] Guid? categoryId = null,
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
            async (page, size) => await _productsService.GetByRestaurantAsync(restaurantId, categoryId, search, active, page, size),
            pageNumber,
            pageSize,
            "Ürünler başarıyla getirildi",
            "Products retrieved successfully");
    }

    [HttpGet("GetProductById")]
    [RequirePermission(Permissions.Menu.View)]
    public async Task<ActionResult<ResponsBase>> GetById([FromQuery] Guid id)
    {
        var product = await _productsService.GetByIdAsync(id);
        if (product == null) return NotFound("Ürün bulunamadı", "Product not found");
        return Success(product, "Ürün başarıyla getirildi", "Product retrieved successfully");
    }

    [HttpPost("AddProduct")]
    [RequirePermission(Permissions.Menu.Create)]
    public async Task<ActionResult<ResponsBase>> Create([FromBody] ProductCreateDto dto)
    {
        var (product, error) = await _productsService.CreateAsync(dto);
        if (product == null) return BadRequest(error ?? "Ürün oluşturulamadı", "Product could not be created");
        return Success(product, "Ürün başarıyla oluşturuldu", "Product created successfully");
    }

    [HttpPut("UpdateProduct")]
    [RequirePermission(Permissions.Menu.Update)]
    public async Task<ActionResult<ResponsBase>> Update([FromQuery] Guid id, [FromBody] ProductUpdateDto dto)
    {
        var ok = await _productsService.UpdateAsync(id, dto);
        if (!ok) return NotFound("Ürün bulunamadı", "Product not found");
        return Success("Ürün başarıyla güncellendi", "Product updated successfully");
    }

    [HttpDelete("DeleteProduct")]
    [RequirePermission(Permissions.Menu.Delete)]
    public async Task<ActionResult<ResponsBase>> Delete([FromQuery] Guid id)
    {
        var ok = await _productsService.DeleteAsync(id);
        if (!ok) return NotFound("Ürün bulunamadı", "Product not found");
        return Success("Ürün başarıyla silindi", "Product deleted successfully");
    }
} 