using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QR_Menu.Application.Admin.DTOs;
using QR_Menu.Application.Common;
using QR_Menu.Application.Users.DTOs;
using QR_Menu.Domain;
using QR_Menu.Infrastructure;

namespace QR_Menu.Application.Admin;

public class AdminService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly UserManager<User> _userManager;

    public AdminService(AppDbContext context, IMapper mapper, RoleManager<IdentityRole<Guid>> roleManager, UserManager<User> userManager)
    {
        _context = context;
        _mapper = mapper;
        _roleManager = roleManager;
        _userManager = userManager;
    }

    #region User Management

    public async Task<(List<AdminUserDto> Users, int TotalCount)> GetUsersAsync(
        string? searchKey = null,
        bool? dealer = null,
        string? role = null,
        string? city = null,
        bool? active = null,
        bool? emailConfirmed = null,
        int pageNumber = 1,
        int pageSize = 10)
    {
        var query = _userManager.Users.AsNoTracking();

        // now i filter them when needed
        if (!string.IsNullOrWhiteSpace(searchKey))
        {
            query = query.Where(u => 
                u.FirstName.Contains(searchKey) ||
                u.LastName.Contains(searchKey) ||
                u.Email.Contains(searchKey) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(searchKey))
            );    
        }

        if (!string.IsNullOrWhiteSpace(role) && Enum.TryParse<UserRole>(role, out var userRole))
        {
            query = query.Where(u => u.Role == userRole);
        }

        if (dealer.HasValue)
        {
            query = query.Where(u => u.IsDealer == dealer.Value);
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(u => u.City.Contains(city));
        }

        if (active.HasValue)
        {
            query = query.Where(u => u.IsActive == active.Value);
        }

        if (emailConfirmed.HasValue)
        {
            query = query.Where(u => u.EmailConfirmed == emailConfirmed.Value);
        }

        var total = await query.CountAsync();

        var users = await query
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminUserDto 
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                Role = u.Role.ToString(),
                IsActive = u.IsActive,
                IsDealer = u.IsDealer,
                EmailConfirmed = u.EmailConfirmed,
                City = u.City,
                District = u.District,
                Neighbourhood = u.Neighbourhood,
                DealerId = u.DealerId,
                Note = u.Note,
                CreatedDateTime = u.CreatedDateTime,
                LastUpdateDateTime = u.LastUpdateDateTime,
                LastLoginDate = u.LastLoginAt,
                AccessFailedCount = u.AccessFailedCount,
                LockoutEnd = u.LockoutEnd,
                RestaurantsCount = u.Restaurants != null ? u.Restaurants.Count : 0,
                LicensesCount = u.Licenses != null ? u.Licenses.Count : 0,
                ActiveRestaurantsCount = u.Restaurants != null ? u.Restaurants.Count(r => r.IsActive) : 0,
                ActiveLicensesCount = u.Licenses != null ? u.Licenses.Count(l => l.IsActive) : 0
            }).ToListAsync();



        // Get dealers names for users that have dealers:
        var dealerIds = users.Where(u => u.DealerId.HasValue).Select(u => u.DealerId!.Value).Distinct().ToList();
        if (dealerIds.Any())
        {
            var dealers = await _userManager.Users
                .Where(d => dealerIds.Contains(d.Id))
                .Select(u => new { u.Id, Name = u.FirstName + " " + u.LastName })
                .ToListAsync();

            foreach (var user in users.Where(u => u.DealerId.HasValue))
            {
                var dealerr = dealers.FirstOrDefault(d => d.Id == user.DealerId);
                user.DealerName = dealerr?.Name;
            }    
        }

        return (users, total);
    }

    public async Task<AdminUserDetailDto?> GetUserDetailAsync(Guid userId)
    {
        var user = await _userManager.Users
            .Include(u => u.Restaurants)
            .ThenInclude(r => r.Categories)
            .Include(u => u.Licenses)
            .ThenInclude(l => l.Restaurant)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return null;

        var result = new AdminUserDetailDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role.ToString(),
            IsActive = user.IsActive,
            IsDealer = user.IsDealer,
            EmailConfirmed = user.EmailConfirmed,
            City = user.City,
            District = user.District,
            Neighbourhood = user.Neighbourhood,
            DealerId = user.DealerId,
            Note = user.Note,
            AccessFailedCount = user.AccessFailedCount,
            LockoutEnd = user.LockoutEnd,
            RestaurantsCount = user.Restaurants?.Count ?? 0,
            LicensesCount = user.Licenses?.Count ?? 0,
            ActiveRestaurantsCount = user.Restaurants?.Count(r => r.IsActive) ?? 0,
            ActiveLicensesCount = user.Licenses?.Count(l => l.IsActive) ?? 0
        };

        // this is restaurants summary
        if (user.Restaurants != null)
        {
            result.Restaurants = user.Restaurants.Select(r => new AdminRestaurantSummaryDto 
            {
                Id = r.Id,
                Name = r.Name,
                City = r.City,
                District = r.District,
                Address = r.Address,
                IsActive = r.IsActive,
                HasLicense = r.License != null,
                LicenseExpiry = r.License?.EndDateTime,
                CategoriesCount = r.Categories?.Count ?? 0,
                ProductsCount = 0 // Will be calculated separately if needed
            }).ToList();
        }

        // this is licenses summary
        if (user.Licenses != null)
        {
            result.Licenses = user.Licenses.Select(l => new AdminLicenseSummaryDto 
            {
                Id = l.Id,
                StartDateTime = l.StartDateTime,
                EndDateTime = l.EndDateTime,
                IsActive = l.IsActive,
                UserPrice = l.UserPrice,
                DealerPrice = l.DealerPrice,
                RestaurantName = l.Restaurant?.Name,
                RestaurantId = l.RestaurantId
            }).ToList();
        }

        return result;
    }

    #endregion

    #region Restaurant Management

    public async Task<(List<AdminRestaurantDto> Restaurants, int TotalCount)> GetRestaurantsAsync(
        string? searchKey = null,
        string? city = null,
        bool? active = null,
        bool? hasLicense = null,
        Guid? ownerId = null,
        Guid? dealerId = null,
        string? district = null,
        string? neighbourhood = null,
        int pageNumber = 1,
        int pageSize = 20)
    {
        var query = _context.Restaurants
            .AsNoTracking()
            .Include(r => r.User)
            .Include(r => r.License)
            .Include(r => r.Categories)
            .ThenInclude(c => c.Products)
            .Include(r => r.Products)
            .Include(r => r.User!.Dealer)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(searchKey))
        {
            query = query.Where(r => 
                r.Name.Contains(searchKey) ||
                r.City.Contains(searchKey) ||
                r.District.Contains(searchKey) ||
                (r.Neighbourhood != null && r.Neighbourhood.Contains(searchKey)) ||
                r.User!.FirstName.Contains(searchKey) ||
                r.User!.LastName.Contains(searchKey) ||
                r.User!.Email.Contains(searchKey)
            );
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(r => r.City.ToLower() == city.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(district))
        {
            query = query.Where(r => r.District.ToLower() == district.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(neighbourhood))
        {
            query = query.Where(r => r.Neighbourhood != null && r.Neighbourhood.ToLower() == neighbourhood.ToLower());
        }

        if (active.HasValue)
        {
            query = query.Where(r => r.IsActive == active.Value);
        }

        if (hasLicense.HasValue)
        {
            if (hasLicense.Value)
            {
                query = query.Where(r => r.License != null && r.License.IsActive);
            }
            else
            {
                query = query.Where(r => r.License == null || !r.License.IsActive);
            }
        }

        if (ownerId.HasValue)
        {
            query = query.Where(r => r.UserId == ownerId.Value);
        }

        if (dealerId.HasValue)
        {
            query = query.Where(r => r.DealerId == dealerId.Value);
        }

        var total = await query.CountAsync();

        var restaurants = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new AdminRestaurantDto
            {
                Id = r.Id,
                Name = r.Name,
                Telefon = r.Telefon,
                City = r.City,
                District = r.District,
                Neighbourhood = r.Neighbourhood,
                Address = r.Address,
                Lat = r.Lat,
                Lng = r.Lng,
                IsActive = r.IsActive,
                WorkingHours = r.WorkingHours,
                MinDistance = r.MinDistance,
                GoogleAnalytics = r.GoogleAnalytics,
                DefaultLang = r.DefaultLang,
                InPersonOrder = r.InPersonOrder,
                OnlineOrder = r.OnlineOrder,
                Slogan1 = r.Slogan1,
                Slogan2 = r.Slogan2,
                Hide = r.Hide,
                CreatedAt = r.CreatedAt,

                // Owner information
                UserId = r.UserId,
                OwnerName = r.User!.FullName,
                OwnerEmail = r.User!.Email,
                OwnerPhone = r.User!.PhoneNumber,
                OwnerRole = r.User!.Role.ToString(),
                OwnerIsActive = r.User!.IsActive,

                // Dealer information
                DealerId = r.DealerId,
                DealerName = r.User!.Dealer != null ? r.User!.Dealer.FullName : null,
                DealerEmail = r.User!.Dealer != null ? r.User!.Dealer.Email : null,

                // License information
                LicenseId = r.LicenseId,
                HasLicense = r.License != null,
                LicenseStart = r.License != null ? r.License.StartDateTime : null,
                LicenseEnd = r.License != null ? r.License.EndDateTime : null,
                LicenseIsActive = r.License != null ? r.License.IsActive : false,
                LicenseIsExpired = r.License != null ? DateTime.UtcNow > r.License.EndDateTime : false,
                LicenseUserPrice = r.License != null ? r.License.UserPrice : null,
                LicenseDealerPrice = r.License != null ? r.License.DealerPrice : null,

                // Statistics
                CategoriesCount = r.Categories != null ? r.Categories.Count : 0,
                ProductsCount = r.Products != null ? r.Products.Count : 0,
                ActiveProductsCount = r.Products != null ? r.Products.Count(p => p.IsActive) : 0,
                OrdersCount = 0, // TODO: Add orders count when Order entity is implemented
                PendingOrdersCount = 0,
                CompletedOrdersCount = 0,
                TotalRevenue = 0, // TODO: Add revenue calculation when Order entity is implemented
                LastOrderDate = null
            })
            .ToListAsync();

        return (restaurants, total);
    }    

    public async Task<AdminRestaurantDetailDto?> GetRestaurantDetailAsync(Guid restaurantId)
    {
        var restaurant = await _context.Restaurants
            .Include(r => r.User)
            .Include(r => r.License)
            .ThenInclude(l => l.User)
            .Include(r => r.Categories)
            .Include(r => r.Products)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == restaurantId);

        if (restaurant == null)
            return null;

        var result = new AdminRestaurantDetailDto 
        {
            Id = restaurant.Id,
            Name = restaurant.Name,
            Telefon = restaurant.Telefon,
            City = restaurant.City,
            District = restaurant.District,
            Neighbourhood = restaurant.Neighbourhood,
            Address = restaurant.Address,
            Lat = restaurant.Lat,
            Lng = restaurant.Lng,
            IsActive = restaurant.IsActive,
            WorkingHours = restaurant.WorkingHours,
            MinDistance = restaurant.MinDistance,
            GoogleAnalytics = restaurant.GoogleAnalytics,
            DefaultLang = restaurant.DefaultLang,
            InPersonOrder = restaurant.InPersonOrder,
            OnlineOrder = restaurant.OnlineOrder,
            Slogan1 = restaurant.Slogan1,
            Slogan2 = restaurant.Slogan2,
            Hide = restaurant.Hide,
            UserId = restaurant.UserId,
            OwnerName = restaurant.User.FirstName + " " + restaurant.User.LastName,
            OwnerEmail = restaurant.User.Email,
            OwnerPhone = restaurant.User.PhoneNumber,
            OwnerRole = restaurant.User.Role.ToString(),
            OwnerIsActive = restaurant.User.IsActive,
            DealerId = restaurant.DealerId,
            DealerName = restaurant.License?.User.FirstName + " " + restaurant.License?.User.LastName,
            DealerEmail = restaurant.License?.User.Email,
            LicenseId = restaurant.LicenseId,
            HasLicense = restaurant.License != null,
            LicenseStart = restaurant.License?.StartDateTime,
            LicenseEnd = restaurant.License?.EndDateTime,
                            LicenseIsActive = restaurant.License?.IsActive ?? false,
                LicenseIsExpired = restaurant.License != null && restaurant.License.EndDateTime <= DateTime.UtcNow,
            LicenseUserPrice = restaurant.License?.UserPrice,
            LicenseDealerPrice = restaurant.License?.DealerPrice,
            CategoriesCount = restaurant.Categories?.Count ?? 0,
            ProductsCount = restaurant.Categories?.Sum(c => c.Products?.Count ?? 0) ?? 0,
            ActiveProductsCount = restaurant.Categories?.Sum(c => c.Products?.Count(p => p.IsActive) ?? 0) ?? 0
        };

        // Get categories too:
        if (restaurant.Categories != null)
        {
            result.Categories = restaurant.Categories.Select(c => new AdminCategoryDto 
            {
                Id = c.Id,
                Name = c.Name,
                IsActive = c.IsActive,
                ProductsCount = c.Products?.Count ?? 0,
                ActiveProductsCount = c.Products?.Count(p => p.IsActive) ?? 0
            }).ToList();
        }

        if (restaurant.License != null)
        {
            result.License = new AdminLicenseDto
            {
                Id = restaurant.License.Id,
                StartDateTime = restaurant.License.StartDateTime,
                EndDateTime = restaurant.License.EndDateTime,
                IsActive = restaurant.License.IsActive,
                UserPrice = restaurant.License.UserPrice,
                DealerPrice = restaurant.License.DealerPrice,
                UserId = restaurant.License.UserId,
                UserName = restaurant.License.User.FirstName + " " + restaurant.License.User.LastName,
                UserEmail = restaurant.License.User.Email,
                UserPhone = restaurant.License.User.PhoneNumber,
                UserIsActive = restaurant.License.User.IsActive,
                UserIsDealer = restaurant.License.User.IsDealer,
                RestaurantId = restaurant.Id,
                RestaurantName = restaurant.Name,
                RestaurantCity = restaurant.City,
                RestaurantDistrict = restaurant.District,
                RestaurantIsActive = restaurant.IsActive,
                RestaurantOwnerName = restaurant.User.FirstName + " " + restaurant.User.LastName,
                RestaurantOwnerEmail = restaurant.User.Email
            };
        }

        return result;
    }

    #endregion

    #region Restaurant Management - Admin Operations

    public async Task<(bool success, string? errorMessage)> UpdateRestaurantWithDealerAsync(AdminRestaurantUpdateDto request, Guid restaurantId, Guid userId)
    {
        // Validate restaurant exists
        var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.Id == restaurantId);
        if (restaurant == null)
            return (false, "Restoran bulunamadı.");

        // Validate user exists
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return (false, "Kullanıcı bulunamadı.");

        // Validate dealer exists if dealerId is provided
        if (request.DealerId.HasValue)
        {
            var dealer = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == request.DealerId.Value && u.IsDealer);
            if (dealer == null)
                return (false, "Bayi bulunamadı.");
        }

        // Update restaurant properties
        if (request.Name != null) restaurant.Name = request.Name;
        if (request.Telefon != null) restaurant.Telefon = request.Telefon;
        if (request.City != null) restaurant.City = request.City;
        if (request.District != null) restaurant.District = request.District;
        if (request.Neighbourhood != null) restaurant.Neighbourhood = request.Neighbourhood;
        if (request.Address != null) restaurant.Address = request.Address;
        if (request.Lat.HasValue) restaurant.Lat = request.Lat.Value;
        if (request.Lng.HasValue) restaurant.Lng = request.Lng.Value;
        if (request.IsActive.HasValue) restaurant.IsActive = request.IsActive.Value;
        if (request.WorkingHours != null) restaurant.WorkingHours = request.WorkingHours;
        if (request.MinDistance.HasValue) restaurant.MinDistance = request.MinDistance.Value;
        if (request.GoogleAnalytics != null) restaurant.GoogleAnalytics = request.GoogleAnalytics;
        if (request.DefaultLang != null) restaurant.DefaultLang = request.DefaultLang;
        if (request.InPersonOrder.HasValue) restaurant.InPersonOrder = request.InPersonOrder.Value;
        if (request.OnlineOrder.HasValue) restaurant.OnlineOrder = request.OnlineOrder.Value;
        if (request.Slogan1 != null) restaurant.Slogan1 = request.Slogan1;
        if (request.Slogan2 != null) restaurant.Slogan2 = request.Slogan2;
        if (request.Hide.HasValue) restaurant.Hide = request.Hide.Value;

        // Update dealer assignment if provided
        if (request.DealerId.HasValue)
        {
            restaurant.DealerId = request.DealerId.Value;
        }

        // Update user assignment if provided
        if (request.UserId.HasValue)
        {
            restaurant.UserId = request.UserId.Value;
        }

        await _context.SaveChangesAsync();
        return (true, null);
    }

    #endregion

   

    #region License Management

    public async Task<(List<AdminLicenseDto> Licenses, int TotalCount)> GetLicensesAsync(
        string? search = null,
        bool? isActive = null,
        bool? isExpired = null,
        Guid? userId = null,
        Guid? restaurantId = null,
        bool? isSettingsAdded = null,
        int? licenseTypeId = null,
        int? dateRange = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = _context.Licenses
            .Include(l => l.User) // l.User = The inspector/admin
            .Include(l => l.Restaurant)
            .ThenInclude(r => r.User) // l.Restaurant.User = The restaurant owner
            .Include(l => l.LicensePackage)
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

        // New filter: isSettingsAdded (check if license has associated restaurant with settings)
        if (isSettingsAdded.HasValue)
        {
            if (isSettingsAdded.Value)
            {
                query = query.Where(l => l.RestaurantId != null);
            }
            else
            {
                query = query.Where(l => l.RestaurantId == null);
            }
        }

        // New filter: licenseTypeId (filter by license package type)
        if (licenseTypeId.HasValue)
        {
            query = query.Where(l => l.LicensePackage != null && l.LicensePackage.LicenseTypeId == licenseTypeId.Value);
        }

        // New filter: dateRange (filter by date ranges)
        if (dateRange.HasValue)
        {
            var now = DateTime.UtcNow;
            var startDate = dateRange.Value switch
            {
                0 => now.Date, // Today
                1 => now.Date.AddDays(-1), // Yesterday
                2 => now.Date.AddDays(-7), // Last 7 days
                3 => now.Date.AddDays(-30), // Last 30 days
                4 => now.Date.AddDays(-90), // Last 90 days
                5 => now.Date.AddDays(-180), // Last 180 days
                6 => now.Date.AddDays(-365), // Last 365 days
                7 => DateTime.MinValue, // All time
                _ => DateTime.MinValue
            };

            if (dateRange.Value != 7) // Not "All time"
            {
                query = query.Where(l => l.StartDateTime >= startDate);
            }
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

    public async Task<AdminLicenseDto?> GetLicenseByIdAsync(Guid id)
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

    public async Task<ResponsBase> CreateLicenseAsync(AdminLicenseCreateDto dto)
    {
        // Validate restaurant exists
        var restaurant = await _context.Restaurants
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == dto.RestaurantId);
        if (restaurant == null)
            return ResponsBase.Create("Restoran bulunamadı.", "Restaurant not found.", "404");

        // Validate user exists
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == dto.UserId);
        if (user == null)
            return ResponsBase.Create("Kullanıcı bulunamadı.", "User not found.", "404");

        // Validate license package exists
        var licensePackage = await _context.LicensePackages.FirstOrDefaultAsync(lp => lp.Id == dto.LicensePackageId);
        if (licensePackage == null)
            return ResponsBase.Create("Lisans Paketi bulunamadı.", "License Package not found.", "404");

        // Calculate EndDateTime based on StartDateTime and LicensePackageTime
        var endDateTime = dto.LicensePackageTime > 0 
            ? dto.StartDateTime.AddDays(dto.LicensePackageTime)
            : dto.StartDateTime; // If LicensePackageTime is 0, end date equals start date

        var license = new License
        {
            Id = Guid.NewGuid(),
            UserId = dto.UserId,
            RestaurantId = dto.RestaurantId,
            LicensePackageId = dto.LicensePackageId,
            StartDateTime = dto.StartDateTime,
            EndDateTime = endDateTime, // Use calculated end date
            IsActive = dto.IsActive,
            UserPrice = licensePackage.UserPrice,
            DealerPrice = licensePackage.DealerPrice,
        };

        _context.Licenses.Add(license);
        await _context.SaveChangesAsync();

        // Return the created license with user info and restaurant info
        var createdLicense = await GetLicenseByIdAsync(license.Id);
        return ResponsBase.Create("Lisans eklendi", "Added license", "200", createdLicense);
    }

    public async Task<bool> UpdateLicenseAsync(Guid id, AdminLicenseUpdateDto dto)
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

    public async Task<bool> DeleteLicenseAsync(Guid id)
    {
        var license = await _context.Licenses.FindAsync(id);
        if (license == null)
            return false;

        _context.Licenses.Remove(license);
        await _context.SaveChangesAsync();
        return true;
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

    #endregion

    #region License Package Management

    public async Task<(List<AdminLicensePackageDto> Packages, int TotalCount)> GetLicensePackagesAsync(
        string? search = null,
        bool? isActive = null,
        int? licenseTypeId = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = _context.LicensePackages
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(lp => lp.Description != null && lp.Description.Contains(search));
        }

        if (isActive.HasValue)
        {
            query = query.Where(lp => lp.IsActive == isActive.Value);
        }

        if (licenseTypeId.HasValue)
        {
            query = query.Where(lp => lp.LicenseTypeId == licenseTypeId.Value);
        }

        var total = await query.CountAsync();
        var packages = await query
            .OrderByDescending(lp => lp.CreatedDateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(lp => new AdminLicensePackageDto
            {
                Id = lp.Id,
                EntityGuid = lp.EntityGuid,
                LicenseTypeId = lp.LicenseTypeId,
                Time = lp.Time,
                UserPrice = lp.UserPrice,
                DealerPrice = lp.DealerPrice,
                Description = lp.Description,
                IsActive = lp.IsActive,
                CreatedDateTime = lp.CreatedDateTime,
                LastUpdateDateTime = lp.LastUpdateDateTime,
                LicensesCount = lp.Licenses != null ? lp.Licenses.Count : 0,
                ActiveLicensesCount = lp.Licenses != null ? lp.Licenses.Count(l => l.IsActive) : 0,
                TotalRevenue = lp.Licenses != null ? lp.Licenses.Sum(l => l.UserPrice ?? 0) + lp.Licenses.Sum(l => l.DealerPrice ?? 0) : 0,
                MonthlyRevenue = lp.Licenses != null ? lp.Licenses.Where(l => l.StartDateTime >= DateTime.UtcNow.AddDays(-30))
                    .Sum(l => l.UserPrice ?? 0) + lp.Licenses.Where(l => l.StartDateTime >= DateTime.UtcNow.AddDays(-30))
                    .Sum(l => l.DealerPrice ?? 0) : 0
            }).ToListAsync();

        return (packages, total);
    }

    public async Task<AdminLicensePackageDto?> GetLicensePackageByIdAsync(Guid id)
    {
        var package = await _context.LicensePackages
            .Include(lp => lp.Licenses)
            .AsNoTracking()
            .FirstOrDefaultAsync(lp => lp.Id == id);

        if (package == null)
            return null;

        return new AdminLicensePackageDto
        {
            Id = package.Id,
            EntityGuid = package.EntityGuid,
            LicenseTypeId = package.LicenseTypeId,
            Time = package.Time,
            UserPrice = package.UserPrice,
            DealerPrice = package.DealerPrice,
            Description = package.Description,
            IsActive = package.IsActive,
            CreatedDateTime = package.CreatedDateTime,
            LastUpdateDateTime = package.LastUpdateDateTime,
            LicensesCount = package.Licenses?.Count ?? 0,
            ActiveLicensesCount = package.Licenses?.Count(l => l.IsActive) ?? 0,
            TotalRevenue = package.Licenses?.Sum(l => l.UserPrice ?? 0) + package.Licenses?.Sum(l => l.DealerPrice ?? 0) ?? 0,
            MonthlyRevenue = package.Licenses?.Where(l => l.StartDateTime >= DateTime.UtcNow.AddDays(-30))
                .Sum(l => l.UserPrice ?? 0) + package.Licenses?.Where(l => l.StartDateTime >= DateTime.UtcNow.AddDays(-30))
                .Sum(l => l.DealerPrice ?? 0) ?? 0
        };
    }

    public async Task<AdminLicensePackageDto> CreateLicensePackageAsync(AdminLicensePackageCreateDto dto)
    {
        var package = new LicensePackage
        {
            Id = Guid.NewGuid(),
            EntityGuid = dto.EntityGuid,
            LicenseTypeId = dto.LicenseTypeId,
            Time = dto.Time,
            UserPrice = dto.UserPrice,
            DealerPrice = dto.DealerPrice,
            Description = dto.Description,
            IsActive = dto.IsActive,
            CreatedDateTime = DateTime.UtcNow,
            LastUpdateDateTime = DateTime.UtcNow
        };

        _context.LicensePackages.Add(package);
        await _context.SaveChangesAsync();

        var createdPackage = await GetLicensePackageByIdAsync(package.Id);
        return createdPackage!;
    }

    public async Task<bool> UpdateLicensePackageAsync(Guid id, AdminLicensePackageUpdateDto dto)
    {
        var package = await _context.LicensePackages.FindAsync(id);
        if (package == null)
            return false;

        if (dto.EntityGuid.HasValue) package.EntityGuid = dto.EntityGuid.Value;
        if (dto.LicenseTypeId.HasValue) package.LicenseTypeId = dto.LicenseTypeId.Value;
        if (dto.Time.HasValue) package.Time = dto.Time.Value;
        if (dto.UserPrice.HasValue) package.UserPrice = dto.UserPrice.Value;
        if (dto.DealerPrice.HasValue) package.DealerPrice = dto.DealerPrice.Value;
        if (dto.Description != null) package.Description = dto.Description;
        if (dto.IsActive.HasValue) package.IsActive = dto.IsActive.Value;
        
        package.LastUpdateDateTime = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteLicensePackageAsync(Guid id)
    {
        var package = await _context.LicensePackages.FindAsync(id);
        if (package == null)
            return false;

        // Check if package has any licenses
        var hasLicenses = await _context.Licenses.AnyAsync(l => l.LicensePackageId == id);
        if (hasLicenses)
        {
            return false; // Cannot delete package with existing licenses
        }

        _context.LicensePackages.Remove(package);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<AdminLicensePackageDto>> GetActiveLicensePackagesAsync()
    {
        var packages = await _context.LicensePackages
            .Where(lp => lp.IsActive)
            .AsNoTracking()
            .Select(lp => new AdminLicensePackageDto
            {
                Id = lp.Id,
                EntityGuid = lp.EntityGuid,
                LicenseTypeId = lp.LicenseTypeId,
                Time = lp.Time,
                UserPrice = lp.UserPrice,
                DealerPrice = lp.DealerPrice,
                Description = lp.Description,
                IsActive = lp.IsActive,
                CreatedDateTime = lp.CreatedDateTime,
                LastUpdateDateTime = lp.LastUpdateDateTime,
                LicensesCount = 0,
                ActiveLicensesCount = 0,
                TotalRevenue = 0,
                MonthlyRevenue = 0
            }).OrderBy(lp => lp.LicenseTypeId)
            .ThenBy(lp => lp.Time)
            .ToListAsync();

        return packages;
    }

    #endregion
}


