using Microsoft.AspNetCore.Mvc;
using QR_Menu.Application.Licenses;
using QR_Menu.Infrastructure.Authorization;
using QR_Menu.Domain.Common;
using QR_Menu.Application.Admin.DTOs;
using QR_Menu.Application.Users.DTOs;
using System.Security.Claims;
using System.Net;

namespace QR_Menu.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LicenseController : ControllerBase
{
    private readonly LicenseService _licenseService;

    public LicenseController(LicenseService licenseService)
    {
        _licenseService = licenseService;
    }

    [HttpGet("GetAllLicenses")]
    [RequirePermission(Permissions.Licenses.ViewAll)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<object>> GetAllLicenses(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isExpired,
        [FromQuery] Guid? userId,
        [FromQuery] Guid? restaurantId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var (licenses, total) = await _licenseService.GetAllAsync(
            search, isActive, isExpired, userId, restaurantId, page, pageSize);
        return Ok(new { 
            total, 
            licenses, 
            page, 
            pageSize, 
            totalPages = (int)Math.Ceiling((double)total / pageSize) 
        });
    }

    [HttpGet("GetMyLicenses")]
    [RequirePermission(Permissions.Licenses.ViewOwn)]
    public async Task<ActionResult<object>> GetMyLicenses(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isExpired,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (userIdStr == null || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized(new { message = "Invalid user." });

        var (licenses, total) = await _licenseService.GetAllAsync(
            search, isActive, isExpired, userId, null, page, pageSize);
        return Ok(new { total, licenses });
    }

    [HttpGet("GetLicenseDetailById")]
    [RequirePermission(Permissions.Licenses.View)]
    public async Task<ActionResult<AdminLicenseDto>> GetLicenseDetail(Guid id)
    {
        var license = await _licenseService.GetByIdAsync(id);
        if (license == null) return NotFound(new { message = "License not found" });
        return Ok(license);
    }

    [HttpPost("CreateLicense")]
    [RequirePermission(Permissions.Licenses.Create)]
    public async Task<ActionResult<AdminLicenseDto>> CreateLicense([FromBody] AdminLicenseCreateDto dto)
    {
        var license = await _licenseService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetLicenseDetail), new { id = license.Id }, license);
    }

    [HttpPut("UpdateLicenseById")]
    [RequirePermission(Permissions.Licenses.Update)]
    public async Task<IActionResult> UpdateLicense(Guid id, [FromBody] AdminLicenseUpdateDto dto)
    {
        var success = await _licenseService.UpdateAsync(id, dto);
        if (!success) return NotFound(new { message = "License not found" });
        return NoContent();
    }

    [HttpDelete("DeleteLicenseById")]
    [RequirePermission(Permissions.Licenses.Delete)]
    public async Task<IActionResult> DeleteLicense(Guid id)
    {
        var success = await _licenseService.DeleteAsync(id);
        if (!success) return NotFound(new { message = "License not found" });
        return NoContent();
    }

    [HttpPost("ExtendLicenseById")]
    [RequirePermission(Permissions.Licenses.Extend)]
    public async Task<IActionResult> ExtendLicense(Guid id, [FromBody] ExtendLicenseDto dto)
    {
        var success = await _licenseService.ExtendLicenseAsync(id, dto.NewEndDate);
        if (!success) return NotFound(new { message = "License not found" });
        return Ok(new { message = "License extended successfully" });
    }

    [HttpPost("ActivateLicenseById")]
    [RequirePermission(Permissions.Licenses.Activate)]
    public async Task<IActionResult> ActivateLicense(Guid id)
    {
        var success = await _licenseService.ActivateLicenseAsync(id);
        if (!success) return NotFound(new { message = "License not found" });
        return Ok(new { message = "License activated successfully" });
    }

    [HttpPost("DeactivateLicenseById")]
    [RequirePermission(Permissions.Licenses.Deactivate)]
    public async Task<IActionResult> DeactivateLicense(Guid id)
    {
        var success = await _licenseService.DeactivateLicenseAsync(id);
        if (!success) return NotFound(new { message = "License not found" });
        return Ok(new { message = "License deactivated successfully" });
    }

    [HttpGet("stats")]
    [RequirePermission(Permissions.Dashboard.ViewLicenseStats)]
    public async Task<ActionResult<AdminLicenseStatsDto>> GetLicenseStats()
    {
        var stats = await _licenseService.GetStatsAsync();
        return Ok(stats);
    }

    [HttpGet("GetUserLicensesById")]
    [RequirePermission(Permissions.Licenses.View)]
    public async Task<ActionResult<List<AdminLicenseDto>>> GetUserLicenses(Guid userId)
    {
        var licenses = await _licenseService.GetUserLicensesAsync(userId);
        return Ok(licenses);
    }

    [HttpGet("GetRestaurantLicensesById")]
    [RequirePermission(Permissions.Licenses.View)]
    public async Task<ActionResult<List<AdminLicenseDto>>> GetRestaurantLicenses(Guid restaurantId)
    {
        var licenses = await _licenseService.GetRestaurantLicensesAsync(restaurantId);
        return Ok(licenses);
    }

    [HttpPut("bulk-status")]
    [RequirePermission(Permissions.Licenses.BulkOperations)]
    public async Task<IActionResult> BulkUpdateLicenseStatus([FromBody] BulkStatusUpdateDto dto)
    {
        var successCount = 0;
        foreach (var licenseId in dto.Ids)
        {
            var updateDto = new AdminLicenseUpdateDto { IsActive = dto.IsActive };
            var success = await _licenseService.UpdateAsync(licenseId, updateDto);
            if (success) successCount++;
        }

        return Ok(new { 
            message = $"{successCount} licenses updated successfully", 
            successCount, 
            totalRequested = dto.Ids.Count 
        });
    }

   
}

public class ExtendLicenseDto
{
    public DateTime NewEndDate { get; set; }
} 