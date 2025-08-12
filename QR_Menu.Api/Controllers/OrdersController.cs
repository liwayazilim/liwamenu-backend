using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using QR_Menu.Application.Common;
using QR_Menu.Application.Orders;
using QR_Menu.Application.Orders.DTOs;
using QR_Menu.Domain.Common;
using QR_Menu.Infrastructure.Authorization;
using System.Security.Claims;
using QR_Menu.Application.Admin;

namespace QR_Menu.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : BaseController
{
    private readonly OrdersService _ordersService;
    private readonly AdminService _adminService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(OrdersService ordersService, AdminService adminService, ILogger<OrdersController> logger)
    {
        _ordersService = ordersService;
        _adminService = adminService;
        _logger = logger;
    }

    [HttpPost("CreateOrder")]
    [RequirePermission(Permissions.Orders.Create)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResponsBase>> Create([FromBody] OrderCreateDto dto)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (userIdStr == null || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized("Geçersiz kullanıcı", "Invalid user");

        var (order, error) = await _ordersService.CreateAsync(userId, dto);
        if (order == null) return BadRequest(error ?? "Sipariş oluşturulamadı", "Order could not be created");
        return Success(order, "Sipariş başarıyla oluşturuldu", "Order created successfully");
    }

    [HttpGet("GetOrdersByRestaurantId")]
    [RequirePermission(Permissions.Orders.ViewOwn)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<object>> GetByRestaurant(
        [FromQuery] Guid restaurantId,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
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

        var parsedStatus = Enum.TryParse<QR_Menu.Domain.OrderStatus>(status ?? string.Empty, true, out var st) ? st : (QR_Menu.Domain.OrderStatus?)null;

        return await GetPaginatedDataAsync(
            async (page, size) => await _ordersService.GetByRestaurantAsync(restaurantId, parsedStatus, startDate, endDate, page, size),
            pageNumber,
            pageSize,
            "Siparişler başarıyla getirildi",
            "Orders retrieved successfully");
    }

    [HttpGet("GetOrderById")]
    [RequirePermission(Permissions.Orders.View)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponsBase>> GetById([FromQuery] Guid orderId)
    {
        var order = await _ordersService.GetByIdAsync(orderId);
        if (order == null) return NotFound("Sipariş bulunamadı", "Order not found");
        return Success(order, "Sipariş başarıyla getirildi", "Order retrieved successfully");
    }

    [HttpPut("UpdateOrderStatus")]
    [RequirePermission(Permissions.Orders.ManageStatus)]
    public async Task<ActionResult<ResponsBase>> UpdateStatus([FromQuery] Guid orderId, [FromBody] OrderUpdateStatusDto dto)
    {
        if (!Enum.TryParse<QR_Menu.Domain.OrderStatus>(dto.Status, true, out var newStatus))
            return BadRequest("Geçersiz sipariş durumu", "Invalid order status");

        var success = await _ordersService.UpdateStatusAsync(orderId, newStatus);
        if (!success) return NotFound("Sipariş bulunamadı", "Order not found");
        return Success("Sipariş durumu güncellendi", "Order status updated");
    }
} 