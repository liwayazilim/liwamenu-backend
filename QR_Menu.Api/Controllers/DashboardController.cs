using Microsoft.AspNetCore.Mvc;
using QR_Menu.Application.Admin;
using QR_Menu.Application.Licenses;
using QR_Menu.Infrastructure.Authorization;
using QR_Menu.Domain.Common;
using QR_Menu.Application.Admin.DTOs;
using System.Net;

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
    public async Task<ActionResult<AdminDashboardStatsDto>> GetDashboardStats()
    {
        var stats = await _adminService.GetDashboardStatsAsync();
        return Ok(stats);
    }

/// <summary>
/// Return only basic stats for users with limited permissions
/// </summary>
    [HttpGet("stats/basic")]
    [RequirePermission(Permissions.Dashboard.ViewBasic)]
    public async Task<ActionResult<object>> GetBasicStats()
    {
        var stats = await _adminService.GetDashboardStatsAsync();
        
        // Return only basic stats for users with limited permissions
        return Ok(new
        {
            stats.TotalUsers,
            stats.ActiveUsers,
            stats.TotalRestaurants,
            stats.ActiveRestaurants,
            stats.TotalLicenses,
            stats.ActiveLicenses
        });
    }

    [HttpGet("license-stats")]
    [RequirePermission(Permissions.Dashboard.ViewLicenseStats)]
    public async Task<ActionResult<AdminLicenseStatsDto>> GetLicenseStats()
    {
        var stats = await _licenseService.GetStatsAsync();
        return Ok(stats);
    }

    [HttpGet("user-stats")]
    [RequirePermission(Permissions.Dashboard.ViewUserStats)]
    public async Task<ActionResult<object>> GetUserStats()
    {
        var stats = await _adminService.GetDashboardStatsAsync();
        var roleStats = await _adminService.GetUserRoleStatsAsync();
        
        return Ok(new
        {
            stats.TotalUsers,
            stats.ActiveUsers,
            stats.VerifiedUsers,
            stats.NewUsersThisMonth,
            UsersByRole = roleStats
        });
    }

    [HttpGet("restaurant-stats")]
    [RequirePermission(Permissions.Dashboard.ViewRestaurantStats)]
    public async Task<ActionResult<object>> GetRestaurantStats()
    {
        var stats = await _adminService.GetDashboardStatsAsync();
        var restaurantAnalytics = await _adminService.GetRestaurantAnalyticsAsync();
        
        return Ok(new
        {
            stats.TotalRestaurants,
            stats.ActiveRestaurants,
            stats.RestaurantsWithLicense,
            stats.NewRestaurantsThisMonth,
            TopCities = restaurantAnalytics.GetType().GetProperty("TopCities")?.GetValue(restaurantAnalytics),
            AverageProductsPerRestaurant = restaurantAnalytics.GetType().GetProperty("AverageProductsPerRestaurant")?.GetValue(restaurantAnalytics),
            RestaurantsByCity = restaurantAnalytics.GetType().GetProperty("RestaurantsByCity")?.GetValue(restaurantAnalytics)
        });
    }

    [HttpGet("financial-stats")]
    [RequirePermission(Permissions.Dashboard.ViewFinancials)]
    public async Task<ActionResult<object>> GetFinancialStats()
    {
        var licenseStats = await _licenseService.GetStatsAsync();
        var financialAnalytics = await _adminService.GetFinancialAnalyticsAsync();
        
        return Ok(new
        {
            licenseStats.TotalRevenue,
            licenseStats.MonthlyRevenue,
            licenseStats.MonthlyRevenueBreakdown,
            FinancialAnalytics = financialAnalytics
        });
    }

    [HttpGet("export")]
    [RequirePermission(Permissions.Dashboard.Export)]
    public async Task<ActionResult> ExportDashboardData([FromQuery] string? format = "csv")
    {
        // TODO: Implement export functionality
        return Ok(new { message = "Export functionality will be implemented" });
    }
} 