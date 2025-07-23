using Microsoft.AspNetCore.Mvc;
using QR_Menu.Application.Admin;
using QR_Menu.Application.Licenses;
using QR_Menu.Infrastructure.Authorization;
using QR_Menu.Domain.Common;
using QR_Menu.Application.Common;

namespace QR_Menu.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly AdminService _adminService;
    private readonly LicenseService _licenseService;

    public DashboardController(AdminService adminService, LicenseService licenseService)
    {
        _adminService = adminService;
        _licenseService = licenseService;
    }

    [HttpGet("stats")]
    [RequirePermission(Permissions.Dashboard.ViewAdvanced)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResponsBase>> GetDashboardStats()
    {
        var stats = await _adminService.GetDashboardStatsAsync();
        return Ok(ResponsBase.Create("Dashboard istatistikleri başarıyla alındı", "Dashboard stats retrieved successfully", "200", stats));
    }

    /// <summary>
    /// Return only basic stats for users with limited permissions
    /// </summary>
    [HttpGet("stats/basic")]
    [RequirePermission(Permissions.Dashboard.ViewBasic)]
    public async Task<ActionResult<ResponsBase>> GetBasicStats()
    {
        var stats = await _adminService.GetDashboardStatsAsync();
        var basicStats = new
        {
            TotalUsers = stats.TotalUsers,
            TotalRestaurants = stats.TotalRestaurants,
            TotalLicenses = stats.TotalLicenses,
            TotalOrders = stats.TotalOrders
        };
        return Ok(ResponsBase.Create("Temel istatistikler başarıyla alındı", "Basic stats retrieved successfully", "200", basicStats));
    }

    [HttpGet("license-stats")]
    [RequirePermission(Permissions.Dashboard.ViewLicenseStats)]
    public async Task<ActionResult<ResponsBase>> GetLicenseStats()
    {
        var stats = await _licenseService.GetStatsAsync();
        return Ok(ResponsBase.Create("Lisans istatistikleri başarıyla alındı", "License stats retrieved successfully", "200", stats));
    }

    [HttpGet("user-stats")]
    [RequirePermission(Permissions.Dashboard.ViewUserStats)]
    public async Task<ActionResult<ResponsBase>> GetUserStats()
    {
        var stats = await _adminService.GetUserRoleStatsAsync();
        return Ok(ResponsBase.Create("Kullanıcı istatistikleri başarıyla alındı", "User stats retrieved successfully", "200", stats));
    }

    [HttpGet("restaurant-stats")]
    [RequirePermission(Permissions.Dashboard.ViewRestaurantStats)]
    public async Task<ActionResult<ResponsBase>> GetRestaurantStats()
    {
        var stats = await _adminService.GetRestaurantAnalyticsAsync();
        return Ok(ResponsBase.Create("Restoran istatistikleri başarıyla alındı", "Restaurant stats retrieved successfully", "200", stats));
    }

    [HttpGet("financial-stats")]
    [RequirePermission(Permissions.Dashboard.ViewFinancials)]
    public async Task<ActionResult<ResponsBase>> GetFinancialStats()
    {
        var stats = await _adminService.GetFinancialAnalyticsAsync();
        return Ok(ResponsBase.Create("Finansal istatistikler başarıyla alındı", "Financial stats retrieved successfully", "200", stats));
    }

    [HttpGet("export")]
    [RequirePermission(Permissions.Dashboard.Export)]
    public async Task<ActionResult<ResponsBase>> ExportDashboardData([FromQuery] string? format = "csv")
    {
        // Placeholder for export functionality
        var data = new { format, message = "Export functionality will be implemented" };
        return Ok(ResponsBase.Create("Dışa aktarma başlatıldı", "Export initiated", "200", data));
    }
} 