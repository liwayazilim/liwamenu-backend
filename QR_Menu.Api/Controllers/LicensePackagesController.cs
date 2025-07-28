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
public class LicensePackagesController : BaseController
{
    private readonly AdminService _adminService;

    public LicensePackagesController(AdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("GetLicensePackages")]
    [RequirePermission(Permissions.Licenses.ViewAll)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<object>> GetLicensePackages(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int? pageNumber = null,
        [FromQuery] int? pageSize = null)
    {
        var response = await PaginationHelper.CreatePaginatedResponseAsync(
            dataProvider: async (page, size) => await _adminService.GetLicensePackagesAsync(
                search, isActive, page, size),
            pageNumber,
            pageSize,
            "Lisans paketleri başarıyla alındı",
            "License packages retrieved successfully",
            "Lisans paketi bulunamadı",
            "No license packages found"
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

   

    [HttpGet("GetLicensePackageById")]
    [RequirePermission(Permissions.Licenses.View)]
    public async Task<ActionResult<ResponsBase>> GetLicensePackageById(Guid id)
    {
        var package = await _adminService.GetLicensePackageByIdAsync(id);
        if (package == null) 
            return NotFound(ResponsBase.Create("Lisans paketi bulunamadı", "License package not found", "404"));
        
        return Ok(ResponsBase.Create("Lisans paketi detayları başarıyla alındı", "License package details retrieved successfully", "200", package));
    }

    [HttpPost("AddLicensePackages")]
    [RequirePermission(Permissions.Licenses.Create)]
    public async Task<ActionResult<ResponsBase>> AddLicensePackages([FromBody] AdminLicensePackageCreateDto dto)
    {
        var (package, errorMessage) = await _adminService.CreateLicensePackageAsync(dto);
        if(package == null)
        {
            return BadRequest(errorMessage ?? "Geçersiz istek", "Invalid request");
        }
        return Success(package, "Lisans paketi başarıyla oluşturuldu", "License package created successfully");
    }

    [HttpPut("UpdateLicensePackage")]
    [RequirePermission(Permissions.Licenses.Update)]
    public async Task<ActionResult<ResponsBase>> UpdateLicensePackage(Guid id, [FromBody] AdminLicensePackageUpdateDto dto)
    {
        var success = await _adminService.UpdateLicensePackageAsync(id, dto);
        if (!success) 
            return NotFound(ResponsBase.Create("Lisans paketi bulunamadı", "License package not found", "404"));
        
        return Ok(ResponsBase.Create("Lisans paketi başarıyla güncellendi", "License package updated successfully", "200"));
    }

    [HttpDelete("DeleteLicensePackage")]
    [RequirePermission(Permissions.Licenses.Delete)]
    public async Task<ActionResult<ResponsBase>> DeleteLicensePackage(Guid id)
    {
        var success = await _adminService.DeleteLicensePackageAsync(id);
        if (!success) 
            return BadRequest(ResponsBase.Create("Lisans paketi silinemez. Bu pakete ait lisanslar bulunmaktadır.", "Cannot delete license package. It has associated licenses.", "400"));
        
        return Ok(ResponsBase.Create("Lisans paketi başarıyla silindi", "License package deleted successfully", "200"));
    }

  
} 