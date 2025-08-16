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
        int pageSize = 20,
        string? baseUrl = null)
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
            .OrderByDescending(r => r.CreatedDateTime)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new AdminRestaurantDto
            {
                Id = r.Id,
                Name = r.Name,
                PhoneNumber = r.PhoneNumber,
                City = r.City,
                District = r.District,
                Neighbourhood = r.Neighbourhood,
                Address = r.Address,
                Latitude = r.Latitude,
                Longitude = r.Longitude,
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
                ThemeId = r.ThemeId,
                CreatedDateTime = r.CreatedDateTime,
                LastUpdateDateTime = r.LastUpdateDateTime,

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

                // images infos: 
                ImageFileName = r.ImageFileName,
                ImageContentType = r.ImageContentType,
                ImageAbsoluteUrl = r.ImageFileName != null && baseUrl != null ? baseUrl + "/images/restaurants/" + r.ImageFileName : null,
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

    public async Task<AdminRestaurantDetailDto?> GetRestaurantDetailAsync(Guid restaurantId, string? baseUrl = null)
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
            PhoneNumber = restaurant.PhoneNumber,
            City = restaurant.City,
            District = restaurant.District,
            Neighbourhood = restaurant.Neighbourhood,
            Address = restaurant.Address,
            Latitude = restaurant.Latitude,
            Longitude = restaurant.Longitude,
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
            ThemeId = restaurant.ThemeId,
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

        // image infos
        result.ImageFileName = restaurant.ImageFileName;
        result.ImageContentType = restaurant.ImageContentType;
        result.ImageAbsoluteUrl = restaurant.ImageFileName != null && baseUrl != null
            ? baseUrl + "/images/restaurants/" + restaurant.ImageFileName
            : null;

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
        if (request.PhoneNumber != null) restaurant.PhoneNumber = request.PhoneNumber;
        if (request.City != null) restaurant.City = request.City;
        if (request.District != null) restaurant.District = request.District;
        if (request.Neighbourhood != null) restaurant.Neighbourhood = request.Neighbourhood;
        if (request.Address != null) restaurant.Address = request.Address;
        if (request.Latitude.HasValue) restaurant.Latitude = request.Latitude.Value;
        if (request.Longitude.HasValue) restaurant.Longitude = request.Longitude.Value;
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
        if (request.ThemeId.HasValue)
        {
            if (request.ThemeId.Value < 0 || request.ThemeId.Value > 14)
                return (false, "Geçersiz tema. Tema 0-14 aralığında olmalıdır.");
            restaurant.ThemeId = request.ThemeId.Value;
        }

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

        // Update LastUpdateDateTime
        restaurant.LastUpdateDateTime = DateTime.UtcNow;

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
        int? dateRange = null,
        int page = 1,
        int pageSize = 20,
        bool ownerOnly = false)
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
            if (ownerOnly)
            {
                query = query.Where(l => l.Restaurant != null && l.Restaurant.UserId == userId.Value);
            }
            else
            {
                query = query.Where(l => l.UserId == userId.Value);
            }
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
                CreatedDateTime = l.CreatedDateTime,
                LastUpdateDateTime = l.LastUpdateDateTime,
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
            CreatedDateTime = license.CreatedDateTime,
            LastUpdateDateTime = license.LastUpdateDateTime,
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
            StartDateTime = DateTime.SpecifyKind(dto.StartDateTime, DateTimeKind.Utc),
            EndDateTime = DateTime.SpecifyKind(endDateTime, DateTimeKind.Utc), // Use calculated end date
            IsActive = dto.IsActive,
            UserPrice = licensePackage.UserPrice,
            DealerPrice = licensePackage.DealerPrice,
            CreatedDateTime = DateTime.UtcNow,
            LastUpdateDateTime = DateTime.UtcNow
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

        if (dto.StartDateTime.HasValue) license.StartDateTime = DateTime.SpecifyKind(dto.StartDateTime.Value, DateTimeKind.Utc);
        if (dto.EndDateTime.HasValue) license.EndDateTime = DateTime.SpecifyKind(dto.EndDateTime.Value, DateTimeKind.Utc);
        if (dto.IsActive.HasValue) license.IsActive = dto.IsActive.Value;
        if (dto.UserPrice.HasValue) license.UserPrice = dto.UserPrice.Value;
        if (dto.DealerPrice.HasValue) license.DealerPrice = dto.DealerPrice.Value;
        if (dto.RestaurantId.HasValue) license.RestaurantId = dto.RestaurantId.Value;

        // Update LastUpdateDateTime
        license.LastUpdateDateTime = DateTime.UtcNow;

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
                CreatedDateTime = l.CreatedDateTime,
                LastUpdateDateTime = l.LastUpdateDateTime,
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

    public async Task<(bool success, string? errorMessage)> LicenseTransferAsync(Guid licenseId, Guid restaurantId)
    {
        // Check if license exists
        var license = await _context.Licenses.FirstOrDefaultAsync(l => l.Id == licenseId);
        if (license == null)
            return (false, "Lisans bulunamadı.");

        // Check if restaurant exists
        var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.Id == restaurantId);
        if (restaurant == null)
            return (false, "Restoran bulunamadı.");

        // Update license restaurant assignment
        license.RestaurantId = restaurantId;
        license.LastUpdateDateTime = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return (true, null);
    }

   public async Task<(bool success, string? errorMessage)> UpdateLicenseDateAsync(Guid licenseId, DateTime startDateTime, DateTime endDateTime)
   {
        var license = await _context.Licenses.FirstOrDefaultAsync(l => l.Id == licenseId);
        if(license == null)
            return (false, "Lisans bulunamadı");

        // Convert DateTime values to UTC to avoid PostgreSQL issues
        license.StartDateTime = DateTime.SpecifyKind(startDateTime, DateTimeKind.Utc);
        license.EndDateTime = DateTime.SpecifyKind(endDateTime, DateTimeKind.Utc);
        license.LastUpdateDateTime = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return (true, null);
   }

    public async Task<(bool success, string? errorMessage)> UpdateLicenseActiveAsync(Guid LicenseId, bool active)
    {
        var license = await _context.Licenses.FirstOrDefaultAsync(l => l.Id == LicenseId);
        if(license == null)
            return (false, "Lisans bulunamadı");

        license.IsActive = active;
        license.LastUpdateDateTime = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return (true, null);
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
                CreatedDateTime = l.CreatedDateTime,
                LastUpdateDateTime = l.LastUpdateDateTime,
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
        int page = 1,
        int pageSize = 20)
    {
        var query = _context.LicensePackages
            .Include(lp => lp.Licenses)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(lp => lp.Description != null && lp.Description.Contains(search));
        }

        if (isActive.HasValue)
        {
            query = query.Where(lp => lp.IsActive == isActive.Value);
        }



        var total = await query.CountAsync();
        var packages = await query
            .OrderByDescending(lp => lp.CreatedDateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(lp => new AdminLicensePackageDto
            {
                Id = lp.Id,
                Name = lp.Name,
                EntityGuid = lp.EntityGuid,
                Time = lp.Time,
                TimeId = lp.TimeId,
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
            Name = package.Name,
            EntityGuid = package.EntityGuid,
            Time = package.Time,
            TimeId = package.TimeId,
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

    public async Task<(AdminLicensePackageDto? Package, string? ErrorMessage)> CreateLicensePackageAsync(AdminLicensePackageCreateDto dto)
    {
        // Validate request
        if (string.IsNullOrWhiteSpace(dto.Name) 
            
            || (dto.Time <= 0) 
            || (dto.TimeId < 0 || dto.TimeId > 1) 
            || (dto.UserPrice <= 0) 
            || (dto.DealerPrice <= 0))
        {
            return (null, "Geçersiz istek. Tüm gerekli alanlar doldurulmalıdır.");
        }

        // Create license package
        var package = new LicensePackage
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            EntityGuid = dto.EntityGuid,
            Time = dto.Time,
            TimeId = dto.TimeId,
            UserPrice = dto.UserPrice,
            DealerPrice = dto.DealerPrice,
            Description = dto.Description,
            IsActive = dto.IsActive,
            CreatedDateTime = DateTime.UtcNow,
            LastUpdateDateTime = DateTime.UtcNow
        };

        _context.LicensePackages.Add(package);
        await _context.SaveChangesAsync();

        var result = new AdminLicensePackageDto
        {
            Id = package.Id,
            Name = package.Name,
            EntityGuid = package.EntityGuid,
            Time = package.Time,
            TimeId = package.TimeId,
            UserPrice = package.UserPrice,
            DealerPrice = package.DealerPrice,
            Description = package.Description,
            IsActive = package.IsActive,
            CreatedDateTime = package.CreatedDateTime,
            LastUpdateDateTime = package.LastUpdateDateTime,
            LicensesCount = 0, // New package has no licenses yet
            ActiveLicensesCount = 0,
            TotalRevenue = 0,
            MonthlyRevenue = 0
        };
        return (result, null);
    }

    public async Task<bool> UpdateLicensePackageAsync(Guid id, AdminLicensePackageUpdateDto dto)
    {
        var package = await _context.LicensePackages.FindAsync(id);
        if (package == null)
            return false;

        // Manual mapping for better performance and clarity
        if (dto.Name != null) package.Name = dto.Name;
        if (dto.EntityGuid.HasValue) package.EntityGuid = dto.EntityGuid.Value;
        if (dto.Time.HasValue) package.Time = dto.Time.Value;
        if (dto.TimeId.HasValue) package.TimeId = dto.TimeId.Value;
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
            .Include(lp => lp.Licenses)
            .AsNoTracking()
            .OrderBy(lp => lp.Time)
            .Select(lp => new AdminLicensePackageDto
            {
                Id = lp.Id,
                Name = lp.Name,
                EntityGuid = lp.EntityGuid,
                Time = lp.Time,
                TimeId = lp.TimeId,
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

        return packages;
    }

    #endregion
}


