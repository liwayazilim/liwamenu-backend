using Microsoft.AspNetCore.Mvc;
using QR_Menu.Application.Licenses;
using QR_Menu.Application.Admin.DTOs;
using QR_Menu.Application.Users.DTOs;
using QR_Menu.Infrastructure.Authorization;
using QR_Menu.Domain.Common;
using QR_Menu.Application.Common;
using System.Security.Claims;

namespace QR_Menu.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LicensesController : ControllerBase
{
    private readonly LicenseService _licenseService;

    public LicensesController(LicenseService licenseService)
    {
        _licenseService = licenseService;
    }

    [HttpGet("GetLicenses")]
    [RequirePermission(Permissions.Licenses.ViewAll)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<object>> GetAllLicenses(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isExpired,
        [FromQuery] Guid? userId,
        [FromQuery] Guid? restaurantId,
        [FromQuery] int? pageNumber = null,
        [FromQuery] int? pageSize = null)
    {
        var response = await PaginationHelper.CreatePaginatedResponseAsync(
            dataProvider: async (page, size) => await _licenseService.GetAllAsync(
                search, isActive, isExpired, userId, restaurantId, page, size),
            pageNumber,
            pageSize,
            "Lisanslar başarıyla alındı",
            "Licenses retrieved successfully",
            "Lisanslar bulunamadı",
            "Licenses not found"
        );

        // If response is ResponsBase 
        if (response is ResponsBase responsBase)
        {
            return responsBase.StatusCode == "404" ? NotFound(responsBase) : Ok(responsBase);
        }
        
        // If response is data object
        return Ok(response);
    }

    [HttpGet("GetMyLicenses")]
    [RequirePermission(Permissions.Licenses.ViewOwn)]
    public async Task<ActionResult<object>> GetMyLicenses(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isExpired,
        [FromQuery] int? pageNumber = null,
        [FromQuery] int? pageSize = null)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (userIdStr == null || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized(ResponsBase.Create("Geçersiz kullanıcı", "Invalid user", "401"));

        var response = await PaginationHelper.CreatePaginatedResponseAsync(
            dataProvider: async (pageNum, size) => await _licenseService.GetAllAsync(
                search, isActive, isExpired, userId, null, pageNum, size),
            pageNumber,
            pageSize,
            "Lisanslarınız başarıyla alındı",
            "Your licenses retrieved successfully",
            "Lisans bulunamadı",
            "No licenses found"
        );

        // If response is ResponsBase 
        if (response is ResponsBase responsBase)
        {
            return responsBase.StatusCode == "404" ? NotFound(responsBase) : Ok(responsBase);
        }
        
        // If response is data object 
        return Ok(response);
    }

    [HttpGet("GetLicenseDetailById")]
    [RequirePermission(Permissions.Licenses.View)]
    public async Task<ActionResult<ResponsBase>> GetLicenseDetail(Guid id)
    {
        var license = await _licenseService.GetByIdAsync(id);
        if (license == null) return NotFound(ResponsBase.Create("Lisans bulunamadı", "License not found", "404"));
        return Ok(ResponsBase.Create("Lisans detayları başarıyla alındı", "License details retrieved successfully", "200", license));
    }

    [HttpPost("CreateLicense")]
    [RequirePermission(Permissions.Licenses.Create)]
    public async Task<ActionResult<ResponsBase>> CreateLicense([FromBody] AdminLicenseCreateDto dto)
    {
        var license = await _licenseService.CreateAsync(dto);
        return Ok(ResponsBase.Create("Lisans başarıyla oluşturuldu", "License created successfully", "201", license));
    }

    [HttpPut("UpdateLicenseById")]
    [RequirePermission(Permissions.Licenses.Update)]
    public async Task<ActionResult<ResponsBase>> UpdateLicense(Guid id, [FromBody] AdminLicenseUpdateDto dto)
    {
        var success = await _licenseService.UpdateAsync(id, dto);
        if (!success) return NotFound(ResponsBase.Create("Lisans bulunamadı", "License not found", "404"));
        return Ok(ResponsBase.Create("Lisans başarıyla güncellendi", "License updated successfully", "200"));
    }

    [HttpDelete("DeleteLicenseById")]
    [RequirePermission(Permissions.Licenses.Delete)]
    public async Task<ActionResult<ResponsBase>> DeleteLicense(Guid id)
    {
        var success = await _licenseService.DeleteAsync(id);
        if (!success) return NotFound(ResponsBase.Create("Lisans bulunamadı", "License not found", "404"));
        return Ok(ResponsBase.Create("Lisans başarıyla silindi", "License deleted successfully", "200"));
    }

    [HttpPost("ExtendLicenseById")]
    [RequirePermission(Permissions.Licenses.Extend)]
    public async Task<ActionResult<ResponsBase>> ExtendLicense(Guid id, [FromBody] ExtendLicenseDto dto)
    {
        var success = await _licenseService.ExtendLicenseAsync(id, dto.NewEndDate);
        if (!success) return NotFound(ResponsBase.Create("Lisans bulunamadı", "License not found", "404"));
        return Ok(ResponsBase.Create("Lisans başarıyla uzatıldı", "License extended successfully", "200"));
    }

    [HttpPost("ActivateLicenseById")]
    [RequirePermission(Permissions.Licenses.Activate)]
    public async Task<ActionResult<ResponsBase>> ActivateLicense(Guid id)
    {
        var success = await _licenseService.ActivateLicenseAsync(id);
        if (!success) return NotFound(ResponsBase.Create("Lisans bulunamadı", "License not found", "404"));
        return Ok(ResponsBase.Create("Lisans başarıyla aktifleştirildi", "License activated successfully", "200"));
    }

    [HttpPost("DeactivateLicenseById")]
    [RequirePermission(Permissions.Licenses.Deactivate)]
    public async Task<ActionResult<ResponsBase>> DeactivateLicense(Guid id)
    {
        var success = await _licenseService.DeactivateLicenseAsync(id);
        if (!success) return NotFound(ResponsBase.Create("Lisans bulunamadı", "License not found", "404"));
        return Ok(ResponsBase.Create("Lisans başarıyla deaktifleştirildi", "License deactivated successfully", "200"));
    }

    [HttpGet("stats")]
    [RequirePermission(Permissions.Dashboard.ViewLicenseStats)]
    public async Task<ActionResult<ResponsBase>> GetLicenseStats()
    {
        var stats = await _licenseService.GetStatsAsync();
        return Ok(ResponsBase.Create("Lisans istatistikleri başarıyla alındı", "License stats retrieved successfully", "200", stats));
    }

    [HttpGet("GetUserLicensesById")]
    [RequirePermission(Permissions.Licenses.View)]
    public async Task<ActionResult<ResponsBase>> GetUserLicenses(Guid userId)
    {
        var licenses = await _licenseService.GetUserLicensesAsync(userId);
        return Ok(ResponsBase.Create("Kullanıcı lisansları başarıyla alındı", "User licenses retrieved successfully", "200", licenses));
    }

    [HttpGet("GetRestaurantLicensesById")]
    [RequirePermission(Permissions.Licenses.View)]
    public async Task<ActionResult<ResponsBase>> GetRestaurantLicenses(Guid restaurantId)
    {
        var licenses = await _licenseService.GetRestaurantLicensesAsync(restaurantId);
        return Ok(ResponsBase.Create("Restoran lisansları başarıyla alındı", "Restaurant licenses retrieved successfully", "200", licenses));
    }

    [HttpPut("bulk-status")]
    [RequirePermission(Permissions.Licenses.BulkOperations)]
    public async Task<ActionResult<ResponsBase>> BulkUpdateLicenseStatus([FromBody] BulkStatusUpdateDto dto)
    {
        var successCount = 0;
        foreach (var licenseId in dto.Ids)
        {
            var updateDto = new AdminLicenseUpdateDto { IsActive = dto.IsActive };
            var success = await _licenseService.UpdateAsync(licenseId, updateDto);
            if (success) successCount++;
        }
        var data = new {
            message = $"{successCount} lisans başarıyla güncellendi",
            successCount,
            totalRequested = dto.Ids.Count
        };
        return Ok(ResponsBase.Create("Toplu güncelleme tamamlandı", "Bulk update completed", "200", data));
    }
}

public class ExtendLicenseDto
{
    public DateTime NewEndDate { get; set; }
} 