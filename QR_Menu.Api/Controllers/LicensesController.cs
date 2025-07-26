using Microsoft.AspNetCore.Mvc;
using QR_Menu.Application.Admin;
using QR_Menu.Application.Admin.DTOs;
using QR_Menu.Application.Users.DTOs;
using QR_Menu.Infrastructure.Authorization;
using QR_Menu.Domain.Common;
using QR_Menu.Application.Common;
using System.Security.Claims;

namespace QR_Menu.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LicensesController : BaseController
{
    private readonly AdminService _adminService;

    public LicensesController(AdminService adminService)
    {
        _adminService = adminService;
    }

   
    ///<param name="dateRange"> 0: Today, 1: Yesterday, 2: Last 7 days, 3: Last 30 days, 4: Last 90 days, 5: Last 180 days, 6: Last 365 days, 7: All time </param>
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
        [FromQuery] bool? isSettingsAdded,
        [FromQuery] int? licenseTypeId,
        [FromQuery] int? dateRange,
        [FromQuery] int? pageNumber = null,
        [FromQuery] int? pageSize = null)
    {
        var response = await PaginationHelper.CreatePaginatedResponseAsync(
            dataProvider: async (page, size) => await _adminService.GetLicensesAsync(
                search, isActive, isExpired, userId, restaurantId, isSettingsAdded, licenseTypeId, dateRange, page, size),
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
            // Now PaginationHelper always returns 200 status, so we just return Ok
            return Ok(responsBase);
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
        [FromQuery] bool? isSettingsAdded,
        [FromQuery] int? licenseTypeId,
        [FromQuery] int? dateRange,
        [FromQuery] int? pageNumber = null,
        [FromQuery] int? pageSize = null)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (userIdStr == null || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized(ResponsBase.Create("Geçersiz kullanıcı", "Invalid user", "401"));

        var response = await PaginationHelper.CreatePaginatedResponseAsync(
            dataProvider: async (pageNum, size) => await _adminService.GetLicensesAsync(
                search, isActive, isExpired, userId, null, isSettingsAdded, licenseTypeId, dateRange, pageNum, size),
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
            // Now PaginationHelper always returns 200 status, so we just return Ok
            return Ok(responsBase);
        }
        
        // If response is data object 
        return Ok(response);
    }

    [HttpGet("GetLicenseDetailById")]
    [RequirePermission(Permissions.Licenses.View)]
    public async Task<ActionResult<ResponsBase>> GetLicenseDetail(Guid id)
    {
        var license = await _adminService.GetLicenseByIdAsync(id);
        if (license == null) return NotFound("Lisans bulunamadı", "License not found");
        return Success(license, "Lisans detayları başarıyla alındı", "License details retrieved successfully");
    }

    [HttpPost("AddLicense")]
    [RequirePermission(Permissions.Licenses.Create)]
    public async Task<ActionResult<ResponsBase>> AddLicense([FromBody] AdminLicenseCreateDto request)
    {
        var result = await _adminService.CreateLicenseAsync(request);
        
        // Check if the operation was successful based on status code
        if (result.StatusCode == "200")
        {
            return Ok(result);
        }
        else if (result.StatusCode == "404")
        {
            return NotFound(result);
        }
        else
        {
            return BadRequest(result);
        }
    }

    [HttpPut("UpdateLicenseById")]
    [RequirePermission(Permissions.Licenses.Update)]
    public async Task<ActionResult<ResponsBase>> UpdateLicense(Guid id, [FromBody] AdminLicenseUpdateDto dto)
    {
        var success = await _adminService.UpdateLicenseAsync(id, dto);
        if (!success) return NotFound("Lisans bulunamadı", "License not found");
        return Success("Lisans başarıyla güncellendi", "License updated successfully");
    }

    [HttpDelete("DeleteLicenseById")]
    [RequirePermission(Permissions.Licenses.Delete)]
    public async Task<ActionResult<ResponsBase>> DeleteLicense(Guid id)
    {
        var success = await _adminService.DeleteLicenseAsync(id);
        if (!success) return NotFound("Lisans bulunamadı", "License not found");
        return Success("Lisans başarıyla silindi", "License deleted successfully");
    }


    [HttpGet("GetLicensesByUserId")]
    [RequirePermission(Permissions.Licenses.View)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<object>> GetLicensesByUserId(
        [FromQuery] Guid userId,
        [FromQuery] int? pageNumber = null,
        [FromQuery] int? pageSize = null,
        [FromQuery] string? searchKey = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] bool? isSettingsAdded = null,
        [FromQuery] int? licenseTypeId = null,
        [FromQuery] int? dateRange = null)
    {
        var response = await PaginationHelper.CreatePaginatedResponseAsync(
            dataProvider: async (page, size) => await _adminService.GetLicensesAsync(
                searchKey, isActive, null, userId, null, isSettingsAdded, licenseTypeId, dateRange, page, size),
            pageNumber,
            pageSize,
            "Kullanıcının lisansları başarıyla alındı",
            "User licenses retrieved successfully",
            "Kullanıcının lisansı bulunamadı",
            "User licenses not found"
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

  

    [HttpGet("GetLicensesByRestaurantId")]
    [RequirePermission(Permissions.Licenses.View)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<object>> GetLicensesByRestaurantId(
        [FromQuery] Guid restaurantId,
        [FromQuery] int? pageNumber = null,
        [FromQuery] int? pageSize = null,
        [FromQuery] string? searchKey = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] bool? isSettingsAdded = null,
        [FromQuery] int? licenseTypeId = null,
        [FromQuery] int? dateRange = null)
    {
        // Validate restaurant exists
        var restaurant = await _adminService.GetRestaurantDetailAsync(restaurantId);
        if (restaurant == null)
            return NotFound("Restoran bulunamadı.", "Restaurant not found.");

        var response = await PaginationHelper.CreatePaginatedResponseAsync(
            dataProvider: async (page, size) => await _adminService.GetLicensesAsync(
                searchKey, isActive, null, null, restaurantId, isSettingsAdded, licenseTypeId, dateRange, page, size),
            pageNumber,
            pageSize,
            "Restoran lisansları başarıyla alındı",
            "Restaurant licenses retrieved successfully",
            "Restoran lisansları bulunamadı",
            "Restaurant licenses not found"
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

    [HttpPut("UpdateLicenseActive")]
    [RequirePermission(Permissions.Licenses.Update)]
    public async Task<ActionResult<ResponsBase>> UpdateLicenseActive(Guid LicenseId,  bool active)
    {
        var (success, errorMessage) = await _adminService.UpdateLicenseActiveAsync(LicenseId, active);
        if(!success)
        {
            if (errorMessage == "Lisans bulunamadı.")
                return NotFound("Lisans bulunamadı.", "License not found.");
            else
                return BadRequest(errorMessage ?? "Lisans aktiflik durumu güncellenirken hata oluştu.", "Error occurred while updating license active status.");
        }
        return Success("Lisans aktiflik durumu başarıyla güncellendi", "License active status updated successfully");
    }

    [HttpPut("UpdateLicenseDate")]
    [RequirePermission(Permissions.Licenses.Update)]
    public async Task<ActionResult<ResponsBase>> UpdateLicenseDate(Guid licenseId, DateTime startDateTime, DateTime endDateTime)
    {
        var (success, errorMessage) = await _adminService.UpdateLicenseDateAsync(licenseId, startDateTime, endDateTime);
        if(!success)
        {
            if (errorMessage == "Lisans bulunamadı.")
                return NotFound("Lisans bulunamadı.", "License not found.");
            else
                return BadRequest(errorMessage ?? "Lisans tarihi güncellenirken hata oluştu.", "Error occurred while updating license date.");
        }
        return Success("Lisans tarihi başarıyla güncellendi", "License date updated successfully");
    }


    [HttpPut("LicenseTransfer")]
    [RequirePermission(Permissions.Licenses.Update)]
    public async Task<ActionResult<ResponsBase>> LicenseTransfer(Guid licenseId, Guid restaurantId)
    {
        var (success, errorMessage) = await _adminService.LicenseTransferAsync(licenseId, restaurantId);
        
        if (!success)
        {
            if (errorMessage == "Lisans bulunamadı.")
                return NotFound("Lisans bulunamadı.", "License not found.");
            else if (errorMessage == "Restoran bulunamadı.")
                return NotFound("Restoran bulunamadı.", "Restaurant not found.");
            else
                return BadRequest(errorMessage ?? "Lisans transfer edilirken hata oluştu.", "Error occurred while transferring license.");
        }
        
        return Success("Lisans transfer edildi.", "License has been transferred.");
    }

    [HttpPut("bulk-status")]
    [RequirePermission(Permissions.Licenses.BulkOperations)]
    public async Task<ActionResult<ResponsBase>> BulkUpdateLicenseStatus([FromBody] BulkStatusUpdateDto dto)
    {
        var successCount = 0;
        foreach (var licenseId in dto.Ids)
        {
            var updateDto = new AdminLicenseUpdateDto { IsActive = dto.IsActive };
            var success = await _adminService.UpdateLicenseAsync(licenseId, updateDto);
            if (success) successCount++;
        }
        var data = new {
            message = $"{successCount} lisans başarıyla güncellendi",
            successCount,
            totalRequested = dto.Ids.Count
        };
        return Success(data, "Toplu güncelleme tamamlandı", "Bulk update completed");
    }
}

public class ExtendLicenseDto
{
    public DateTime NewEndDate { get; set; }
} 