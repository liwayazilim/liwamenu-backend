using Microsoft.AspNetCore.Mvc;
using QR_Menu.Application.Admin;
using QR_Menu.Application.Admin.DTOs;
using QR_Menu.Application.Users.DTOs;
using QR_Menu.Infrastructure.Authorization;
using QR_Menu.Domain.Common;
using QR_Menu.Application.Common;

namespace QR_Menu.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminManagementController : ControllerBase
{
    private readonly AdminService _adminService;

    public AdminManagementController(AdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("GetAllDealers")]
    [RequirePermission(Permissions.Users.ViewAll)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<object>> GetAllDealers(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] bool? hasLicenses,
        [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null)
    {
        var response = await PaginationHelper.CreatePaginatedResponseAsync(
            dataProvider: async (pageNum, size) => await _adminService.GetDealersAsync(
                search, isActive, hasLicenses, pageNum, size),
            page,
            pageSize,
            "Bayiler başarıyla alındı",
            "Dealers retrieved successfully",
            "Bayiler bulunamadı",
            "Dealers not found"
        );

        // If response is ResponsBase (when both parameters are null), handle it accordingly
        if (response is ResponsBase responsBase)
        {
            return responsBase.StatusCode == "404" ? NotFound(responsBase) : Ok(responsBase);
        }
        
        // If response is data object (when pagination parameters are provided), return it directly
        return Ok(response);
    }

    [HttpGet("GetDealerDetailById")]
    [RequirePermission(Permissions.Users.ViewDetails)]
    public async Task<ActionResult<ResponsBase>> GetDealerDetail(Guid id)
    {
        var dealer = await _adminService.GetDealerDetailAsync(id);
        if (dealer == null) return NotFound(ResponsBase.Create("Bayi bulunamadı", "Dealer not found", "404"));
        return Ok(ResponsBase.Create("Bayi detayları başarıyla alındı", "Dealer details retrieved successfully", "200", dealer));
    }

    [HttpPost("CreateDealer")]
    [RequirePermission(Permissions.Users.Create)]
    public async Task<ActionResult<ResponsBase>> CreateDealer([FromBody] CreateDealerDto dto)
    {
        var dealer = await _adminService.CreateDealerAsync(dto);
        if (dealer == null) return BadRequest(ResponsBase.Create("Bayi oluşturulamadı", "Failed to create dealer", "400"));
        return Ok(ResponsBase.Create("Bayi başarıyla oluşturuldu", "Dealer created successfully", "201", dealer));
    }

    [HttpPut("UpdateDealerById")]
    [RequirePermission(Permissions.Users.Update)]
    public async Task<ActionResult<ResponsBase>> UpdateDealer(Guid id, [FromBody] UpdateDealerDto dto)
    {
        var success = await _adminService.UpdateDealerAsync(id, dto);
        if (!success) return NotFound(ResponsBase.Create("Bayi bulunamadı", "Dealer not found", "404"));
        return Ok(ResponsBase.Create("Bayi başarıyla güncellendi", "Dealer updated successfully", "200"));
    }

    [HttpPost("AssignRestaurantsToDealer/{id}")]
    [RequirePermission(Permissions.Restaurants.ManageOwnership)]
    public async Task<ActionResult<ResponsBase>> AssignRestaurantsToDealer(Guid id, [FromBody] AssignRestaurantsDto dto)
    {
        var success = await _adminService.AssignRestaurantsToDealerAsync(id, dto.RestaurantIds);
        if (!success) return NotFound(ResponsBase.Create("Bayi bulunamadı", "Dealer not found", "404"));
        return Ok(ResponsBase.Create("Restoranlar başarıyla bayie atandı", "Restaurants assigned to dealer successfully", "200"));
    }

    [HttpGet("GetDealerStatsById")]
    [RequirePermission(Permissions.Dashboard.ViewAdvanced)]
    public async Task<ActionResult<ResponsBase>> GetDealerStats(Guid id)
    {
        var stats = await _adminService.GetDealerStatsAsync(id);
        if (stats == null) return NotFound(ResponsBase.Create("Bayi bulunamadı", "Dealer not found", "404"));
        return Ok(ResponsBase.Create("Bayi istatistikleri başarıyla alındı", "Dealer stats retrieved successfully", "200", stats));
    }

    [HttpPut("BulkUpdateDealerStatus")]
    [RequirePermission(Permissions.Users.BulkOperations)]
    public async Task<ActionResult<ResponsBase>> BulkUpdateDealerStatus([FromBody] BulkStatusUpdateDto dto)
    {
        var result = await _adminService.BulkUpdateDealerStatusAsync(dto);
        return Ok(ResponsBase.Create("Toplu güncelleme tamamlandı", "Bulk update completed", "200", result));
    }

    [HttpGet("GetAllOwners")]
    [RequirePermission(Permissions.Users.ViewAll)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<object>> GetAllOwners(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] bool? hasRestaurants,
        [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null)
    {
        var response = await PaginationHelper.CreatePaginatedResponseAsync(
            dataProvider: async (pageNum, size) => await _adminService.GetOwnersAsync(
                search, isActive, hasRestaurants, pageNum, size),
            page,
            pageSize,
            "Sahipler başarıyla alındı",
            "Owners retrieved successfully",
            "Sahipler bulunamadı",
            "Owners not found"
        );

        // If response is ResponsBase (when both parameters are null), handle it accordingly
        if (response is ResponsBase responsBase)
        {
            return responsBase.StatusCode == "404" ? NotFound(responsBase) : Ok(responsBase);
        }
        
        // If response is data object (when pagination parameters are provided), return it directly
        return Ok(response);
    }

    [HttpGet("GetOwnerDetailById")]
    [RequirePermission(Permissions.Users.ViewDetails)]
    public async Task<ActionResult<ResponsBase>> GetOwnerDetail(Guid id)
    {
        var owner = await _adminService.GetOwnerDetailAsync(id);
        if (owner == null) return NotFound(ResponsBase.Create("Sahip bulunamadı", "Owner not found", "404"));
        return Ok(ResponsBase.Create("Sahip detayları başarıyla alındı", "Owner details retrieved successfully", "200", owner));
    }

    [HttpPost("CreateOwner")]
    [RequirePermission(Permissions.Users.Create)]
    public async Task<ActionResult<ResponsBase>> CreateOwner([FromBody] CreateOwnerDto dto)
    {
        var owner = await _adminService.CreateOwnerAsync(dto);
        if (owner == null) return BadRequest(ResponsBase.Create("Sahip oluşturulamadı", "Failed to create owner", "400"));
        return Ok(ResponsBase.Create("Sahip başarıyla oluşturuldu", "Owner created successfully", "201", owner));
    }

    [HttpPut("UpdateOwnerById")]
    [RequirePermission(Permissions.Users.Update)]
    public async Task<ActionResult<ResponsBase>> UpdateOwner(Guid id, [FromBody] UpdateOwnerDto dto)
    {
        var success = await _adminService.UpdateOwnerAsync(id, dto);
        if (!success) return NotFound(ResponsBase.Create("Sahip bulunamadı", "Owner not found", "404"));
        return Ok(ResponsBase.Create("Sahip başarıyla güncellendi", "Owner updated successfully", "200"));
    }

    [HttpPost("AssignDealerToOwner")]
    [RequirePermission(Permissions.Users.Update)]
    public async Task<ActionResult<ResponsBase>> AssignDealerToOwner(Guid id, [FromBody] AssignDealerDto dto)
    {
        var success = await _adminService.AssignDealerToOwnerAsync(id, dto.DealerId);
        if (!success) return NotFound(ResponsBase.Create("Sahip veya bayi bulunamadı", "Owner or dealer not found", "404"));
        return Ok(ResponsBase.Create("Bayi başarıyla sahibe atandı", "Dealer assigned to owner successfully", "200"));
    }

    [HttpGet("GetOwnerStatsById")]
    [RequirePermission(Permissions.Dashboard.ViewAdvanced)]
    public async Task<ActionResult<ResponsBase>> GetOwnerStats(Guid id)
    {
        var stats = await _adminService.GetOwnerStatsAsync(id);
        if (stats == null) return NotFound(ResponsBase.Create("Sahip bulunamadı", "Owner not found", "404"));
        return Ok(ResponsBase.Create("Sahip istatistikleri başarıyla alındı", "Owner stats retrieved successfully", "200", stats));
    }

    [HttpPut("BulkUpdateOwnerStatus")]
    [RequirePermission(Permissions.Users.BulkOperations)]
    public async Task<ActionResult<ResponsBase>> BulkUpdateOwnerStatus([FromBody] BulkStatusUpdateDto dto)
    {
        var result = await _adminService.BulkUpdateOwnerStatusAsync(dto);
        return Ok(ResponsBase.Create("Toplu güncelleme tamamlandı", "Bulk update completed", "200", result));
    }
} 