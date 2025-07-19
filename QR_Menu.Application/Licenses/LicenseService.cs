using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using QR_Menu.Application.Admin.DTOs;
using QR_Menu.Domain;
using QR_Menu.Infrastructure;

namespace QR_Menu.Application.Licenses;

public class LicenseService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public LicenseService(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<(List<AdminLicenseDto> Licenses, int TotalCount)> GetAllAsync(
        string? search = null,
        bool? isActive = null,
        bool? isExpired = null,
        Guid? userId = null,
        Guid? restaurantId = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = _context.Licenses
            .Include(l => l.User) // l.User = The inspector/admin
            .Include(l => l.Restaurant)
            .ThenInclude(r => r.User) // l.Restaurant.User = The restaurant owner
            .AsNoTracking();

        // now the filtering :
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(l => l.User.FirstName.Contains(search) ||
                l.User.LastName.Contains(search) ||
                l.User.Email.Contains(search) ||
                (l.Restaurant != null && l.Restaurant.Name.Contains(search)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(l => l.IsActive == isActive.Value);
        }

        if (isExpired.HasValue)
        {
            var now = DateTime.UtcNow;
            if (isExpired.Value)
            {
                query = query.Where(l => l.EndDateTime <= now);
            }
            else
            {
                query = query.Where(l => l.EndDateTime > now);
            }
        }

        if (userId.HasValue)
        {
            query = query.Where(l => l.UserId == userId.Value);
        }

        if (restaurantId.HasValue)
        {
            query = query.Where(l => l.RestaurantId == restaurantId.Value);
        }

        var total = await query.CountAsync();
        var licenses = await query
            .OrderByDescending(l => l.StartDateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new AdminLicenseDto
            {
                Id = l.Id,
                StartDateTime = l.StartDateTime,
                EndDateTime = l.EndDateTime,
                IsActive = l.IsActive,
                UserPrice = l.UserPrice,
                DealerPrice = l.DealerPrice,
                UserId = l.UserId,
                UserName = l.User.FirstName + " " + l.User.LastName,
                UserEmail = l.User.Email,
                UserPhone = l.User.PhoneNumber,
                UserIsActive = l.User.IsActive,
                UserIsDealer = l.User.IsDealer,
                RestaurantId = l.RestaurantId,
                RestaurantName = l.Restaurant != null ? l.Restaurant.Name : null,
                RestaurantCity = l.Restaurant != null ? l.Restaurant.City : null,
                RestaurantDistrict = l.Restaurant != null ? l.Restaurant.District : null,
                RestaurantIsActive = l.Restaurant != null ? l.Restaurant.IsActive : null,
                RestaurantOwnerName = l.Restaurant != null ? l.Restaurant.User.FirstName + " " + l.Restaurant.User.LastName : null,
                RestaurantOwnerEmail = l.Restaurant != null ? l.Restaurant.User.Email : null
            }).ToListAsync();

        return (licenses, total);
    }

    public async Task<AdminLicenseDto?> GetByIdAsync(Guid id)
    {
        var license = await _context.Licenses
            .Include(l => l.User)
            .Include(l => l.Restaurant)
            .ThenInclude(r => r.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id);

        if (license == null)
            return null;

        return new AdminLicenseDto
        {
            Id = license.Id,
            StartDateTime = license.StartDateTime,
            EndDateTime = license.EndDateTime,
            IsActive = license.IsActive,
            UserPrice = license.UserPrice,
            DealerPrice = license.DealerPrice,
            UserId = license.UserId,
            UserName = license.User.FirstName + " " + license.User.LastName,
            UserEmail = license.User.Email,
            UserPhone = license.User.PhoneNumber,
            UserIsActive = license.User.IsActive,
            UserIsDealer = license.User.IsDealer,
            RestaurantId = license.RestaurantId,
            RestaurantName = license.Restaurant?.Name,
            RestaurantCity = license.Restaurant?.City,
            RestaurantDistrict = license.Restaurant?.District,
            RestaurantIsActive = license.Restaurant?.IsActive,
            RestaurantOwnerName = license.Restaurant?.User.FirstName + " " + license.Restaurant?.User.LastName,
            RestaurantOwnerEmail = license.Restaurant?.User.Email
        };
    }

    public async Task<AdminLicenseDto> CreateAsync(AdminLicenseCreateDto dto)
    {
        var license = new License
        {
            Id = Guid.NewGuid(),
            UserId = dto.UserId,
            RestaurantId = dto.RestaurantId,
            StartDateTime = dto.StartDateTime,
            EndDateTime = dto.EndDateTime,
            IsActive = dto.IsActive,
            UserPrice = dto.UserPrice,
            DealerPrice = dto.DealerPrice,
        };

        _context.Licenses.Add(license);
        await _context.SaveChangesAsync();

        // returning the created license with user info and restaurant info
        var createdLicense = await GetByIdAsync(license.Id);
        return createdLicense!;
    }

    public async Task<bool> UpdateAsync(Guid id, AdminLicenseUpdateDto dto)
    {
        var license = await _context.Licenses.FindAsync(id);
        if (license == null)
            return false;

        if (dto.StartDateTime.HasValue) license.StartDateTime = dto.StartDateTime.Value;
        if (dto.EndDateTime.HasValue) license.EndDateTime = dto.EndDateTime.Value;
        if (dto.IsActive.HasValue) license.IsActive = dto.IsActive.Value;
        if (dto.UserPrice.HasValue) license.UserPrice = dto.UserPrice.Value;
        if (dto.DealerPrice.HasValue) license.DealerPrice = dto.DealerPrice.Value;
        if (dto.RestaurantId.HasValue) license.RestaurantId = dto.RestaurantId.Value;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var license = await _context.Licenses.FindAsync(id);
        if (license == null)
            return false;

        _context.Licenses.Remove(license);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<AdminLicenseStatsDto> GetStatsAsync()
    {
        var now = DateTime.UtcNow;
        var thisMonth = new DateTime(now.Year, now.Month, 1);
        var nextMonth = thisMonth.AddMonths(1);
        var thisWeek = now.AddDays(7);

        var licenses = await _context.Licenses
            .AsNoTracking()
            .ToListAsync();

        var stats = new AdminLicenseStatsDto
        {
            TotalLicenses = licenses.Count,
            ActiveLicenses = licenses.Count(l => l.IsActive && l.EndDateTime > now),
            ExpiredLicenses = licenses.Count(l => l.EndDateTime <= now),
            ExpiringThisMonth = licenses.Count(l => l.EndDateTime > now && l.EndDateTime <= nextMonth),
            ExpiringThisWeek = licenses.Count(l => l.EndDateTime > now && l.EndDateTime <= thisWeek),
            TotalRevenue = (decimal)(licenses.Sum(l => l.UserPrice ?? 0) + licenses.Sum(l => l.DealerPrice ?? 0)),
            MonthlyRevenue = (decimal)(licenses.Where(l => l.StartDateTime >= thisMonth)
                .Sum(l => l.UserPrice ?? 0) + licenses.Where(l => l.StartDateTime >= thisMonth)
                .Sum(l => l.DealerPrice ?? 0))
        };

        // get expiring licenses (this week)
        stats.ExpiringLicenses = await _context.Licenses
            .Include(l => l.User)
            .Include(l => l.Restaurant)
            .Where(l => l.EndDateTime > now && l.EndDateTime <= thisWeek)
            .Select(l => new AdminLicenseExpiryDto
            {
                Id = l.Id,
                UserName = l.User.FirstName + " " + l.User.LastName,
                RestaurantName = l.Restaurant != null ? l.Restaurant.Name : null,
                ExpiryDate = l.EndDateTime,
                DaysRemaining = (int)(l.EndDateTime - now).TotalDays,
                IsExpired = false
            })
            .OrderBy(l => l.ExpiryDate)
            .ToListAsync();

        return stats;
    }

    public async Task<bool> ExtendLicenseAsync(Guid id, DateTime newEndDate)
    {
        var license = await _context.Licenses.FindAsync(id);
        if (license == null)
            return false;

        license.EndDateTime = newEndDate;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ActivateLicenseAsync(Guid id)
    {
        var license = await _context.Licenses.FindAsync(id);
        if (license == null)
            return false;

        license.IsActive = true;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeactivateLicenseAsync(Guid id)
    {
        var license = await _context.Licenses.FindAsync(id);
        if (license == null)
            return false;

        license.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<AdminLicenseDto>> GetUserLicensesAsync(Guid userId)
    {
        var licenses = await _context.Licenses
            .Include(l => l.User)
            .Include(l => l.Restaurant)
            .ThenInclude(r => r.User)
            .Where(l => l.UserId == userId)
            .AsNoTracking()
            .Select(l => new AdminLicenseDto
            {
                Id = l.Id,
                StartDateTime = l.StartDateTime,
                EndDateTime = l.EndDateTime,
                IsActive = l.IsActive,
                UserPrice = l.UserPrice,
                DealerPrice = l.DealerPrice,
                UserId = l.UserId,
                UserName = l.User.FirstName + " " + l.User.LastName,
                UserEmail = l.User.Email,
                UserPhone = l.User.PhoneNumber,
                UserIsActive = l.User.IsActive,
                UserIsDealer = l.User.IsDealer,
                RestaurantId = l.RestaurantId,
                RestaurantName = l.Restaurant != null ? l.Restaurant.Name : null,
                RestaurantCity = l.Restaurant != null ? l.Restaurant.City : null,
                RestaurantDistrict = l.Restaurant != null ? l.Restaurant.District : null,
                RestaurantIsActive = l.Restaurant != null ? l.Restaurant.IsActive : null,
                RestaurantOwnerName = l.Restaurant != null ? l.Restaurant.User.FirstName + " " + l.Restaurant.User.LastName : null,
                RestaurantOwnerEmail = l.Restaurant != null ? l.Restaurant.User.Email : null
            }).OrderByDescending(l => l.StartDateTime)
            .ToListAsync();

        return licenses;
    }

    public async Task<List<AdminLicenseDto>> GetRestaurantLicensesAsync(Guid restaurantId)
    {
        var licenses = await _context.Licenses
            .Include(l => l.User)
            .Include(l => l.Restaurant)
            .ThenInclude(r => r.User)
            .Where(l => l.RestaurantId == restaurantId)
            .AsNoTracking()
            .Select(l => new AdminLicenseDto
            {
                Id = l.Id,
                StartDateTime = l.StartDateTime,
                EndDateTime = l.EndDateTime,
                IsActive = l.IsActive,
                UserPrice = l.UserPrice,
                DealerPrice = l.DealerPrice,
                UserId = l.UserId,
                UserName = l.User.FirstName + " " + l.User.LastName,
                UserEmail = l.User.Email,
                UserPhone = l.User.PhoneNumber,
                UserIsActive = l.User.IsActive,
                UserIsDealer = l.User.IsDealer,
                RestaurantId = l.RestaurantId,
                RestaurantName = l.Restaurant != null ? l.Restaurant.Name : null,
                RestaurantCity = l.Restaurant != null ? l.Restaurant.City : null,
                RestaurantDistrict = l.Restaurant != null ? l.Restaurant.District : null,
                RestaurantIsActive = l.Restaurant != null ? l.Restaurant.IsActive : null,
                RestaurantOwnerName = l.Restaurant != null ? l.Restaurant.User.FirstName + " " + l.Restaurant.User.LastName : null,
                RestaurantOwnerEmail = l.Restaurant != null ? l.Restaurant.User.Email : null
            }).OrderByDescending(l => l.StartDateTime)
            .ToListAsync();

        return licenses;
    }
}     



