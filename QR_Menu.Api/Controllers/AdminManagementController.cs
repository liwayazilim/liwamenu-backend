using Microsoft.AspNetCore.Mvc;
using QR_Menu.Application.Admin;
using QR_Menu.Application.Admin.DTOs;
using QR_Menu.Infrastructure.Authorization;
using QR_Menu.Domain.Common;
using QR_Menu.Application.Users.DTOs;
using System.Security.Claims;

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

    #region Dealer Management

    
    [HttpGet("GetAllDealers")]
    [RequirePermission(Permissions.Users.ViewAll)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<object>> GetAllDealers(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] bool? hasLicenses,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var (dealers, total) = await _adminService.GetDealersAsync(search, isActive, hasLicenses, page, pageSize);
        return Ok(new { 
            total, 
            dealers, 
            page, 
            pageSize, 
            totalPages = (int)Math.Ceiling((double)total / pageSize) 
        });
    }

    
    [HttpGet("GetDealerDetailById")]
    [RequirePermission(Permissions.Users.ViewDetails)]
    public async Task<ActionResult<AdminDealerDetailDto>> GetDealerDetail(Guid id)
    {
        var dealer = await _adminService.GetDealerDetailAsync(id);
        if (dealer == null) return NotFound(new { message = "Dealer not found" });
        return Ok(dealer);
    }

    
    [HttpPost("CreateDealer")]
    [RequirePermission(Permissions.Users.Create)]
    public async Task<ActionResult<AdminUserDto>> CreateDealer([FromBody] CreateDealerDto dto)
    {
        var dealer = await _adminService.CreateDealerAsync(dto);
        if (dealer == null) return BadRequest(new { message = "Failed to create dealer" });
        return CreatedAtAction(nameof(GetDealerDetail), new { id = dealer.Id }, dealer);
    }

    
    [HttpPut("UpdateDealerById")]
    [RequirePermission(Permissions.Users.Update)]
    public async Task<IActionResult> UpdateDealer(Guid id, [FromBody] UpdateDealerDto dto)
    {
        var success = await _adminService.UpdateDealerAsync(id, dto);
        if (!success) return NotFound(new { message = "Dealer not found" });
        return NoContent();
    }

    /// <summary>
    /// Assign restaurants to a dealer
    /// </summary>
    [HttpPost("AssignRestaurantsToDealer/{id}")]
    [RequirePermission(Permissions.Restaurants.ManageOwnership)]
    public async Task<IActionResult> AssignRestaurantsToDealer(Guid id, [FromBody] AssignRestaurantsDto dto)
    {
        var success = await _adminService.AssignRestaurantsToDealerAsync(id, dto.RestaurantIds);
        if (!success) return NotFound(new { message = "Dealer not found" });
        return Ok(new { message = "Restaurants assigned successfully" });
    }

    /// <summary>
    /// Get dealer performance statistics
    /// </summary>
    [HttpGet("GetDealerStatsById")]
    [RequirePermission(Permissions.Dashboard.ViewAdvanced)]
    public async Task<ActionResult<DealerStatsDto>> GetDealerStats(Guid id)
    {
        var stats = await _adminService.GetDealerStatsAsync(id);
        if (stats == null) return NotFound(new { message = "Dealer not found" });
        return Ok(stats);
    }

    #endregion

    #region Owner Management

   
    [HttpGet("GetAllOwners")]
    [RequirePermission(Permissions.Users.ViewAll)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<object>> GetAllOwners(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] bool? hasRestaurants,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var (owners, total) = await _adminService.GetOwnersAsync(search, isActive, hasRestaurants, page, pageSize);
        return Ok(new { 
            total, 
            owners, 
            page, 
            pageSize, 
            totalPages = (int)Math.Ceiling((double)total / pageSize) 
        });
    }

    
    [HttpGet("GetOwnerDetailById")]
    [RequirePermission(Permissions.Users.ViewDetails)]
    public async Task<ActionResult<AdminOwnerDetailDto>> GetOwnerDetail(Guid id)
    {
        var owner = await _adminService.GetOwnerDetailAsync(id);
        if (owner == null) return NotFound(new { message = "Owner not found" });
        return Ok(owner);
    }

   
    [HttpPost("CreateOwner")]
    [RequirePermission(Permissions.Users.Create)]
    public async Task<ActionResult<AdminUserDto>> CreateOwner([FromBody] CreateOwnerDto dto)
    {
        var owner = await _adminService.CreateOwnerAsync(dto);
        if (owner == null) return BadRequest(new { message = "Failed to create owner" });
        return CreatedAtAction(nameof(GetOwnerDetail), new { id = owner.Id }, owner);
    }

    
    [HttpPut("UpdateOwnerById")]
    [RequirePermission(Permissions.Users.Update)]
    public async Task<IActionResult> UpdateOwner(Guid id, [FromBody] UpdateOwnerDto dto)
    {
        var success = await _adminService.UpdateOwnerAsync(id, dto);
        if (!success) return NotFound(new { message = "Owner not found" });
        return NoContent();
    }

    
    [HttpPost("AssignDealerToOwner")]
    [RequirePermission(Permissions.Users.Update)]
    public async Task<IActionResult> AssignDealerToOwner(Guid id, [FromBody] AssignDealerDto dto)
    {
        var success = await _adminService.AssignDealerToOwnerAsync(id, dto.DealerId);
        if (!success) return NotFound(new { message = "Owner or dealer not found" });
        return Ok(new { message = "Dealer assigned successfully" });
    }

    /// <summary>
    /// Get owner performance statistics
    /// </summary>
    [HttpGet("GetOwnerStatsById")]
    [RequirePermission(Permissions.Dashboard.ViewAdvanced)]
    public async Task<ActionResult<OwnerStatsDto>> GetOwnerStats(Guid id)
    {
        var stats = await _adminService.GetOwnerStatsAsync(id);
        if (stats == null) return NotFound(new { message = "Owner not found" });
        return Ok(stats);
    }

    #endregion

    #region Bulk Operations

    /// <summary>
    /// Bulk update dealer status
    /// </summary>
    [HttpPut("BulkUpdateDealerStatus")]
    [RequirePermission(Permissions.Users.BulkOperations)]
    public async Task<IActionResult> BulkUpdateDealerStatus([FromBody] BulkStatusUpdateDto dto)
    {
        var result = await _adminService.BulkUpdateDealerStatusAsync(dto);
        return Ok(result);
    }

    /// <summary>
    /// Bulk update owner status
    /// </summary>
    [HttpPut("BulkUpdateOwnerStatus")]
    [RequirePermission(Permissions.Users.BulkOperations)]
    public async Task<IActionResult> BulkUpdateOwnerStatus([FromBody] BulkStatusUpdateDto dto)
    {
        var result = await _adminService.BulkUpdateOwnerStatusAsync(dto);
        return Ok(result);
    }

    #endregion
} 