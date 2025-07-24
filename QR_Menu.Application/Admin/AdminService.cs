using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QR_Menu.Application.Admin.DTOs;
using QR_Menu.Application.Licenses;
using QR_Menu.Application.Users.DTOs;
using QR_Menu.Domain;
using QR_Menu.Infrastructure;

namespace QR_Menu.Application.Admin;

public class AdminService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly LicenseService _licenseService;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly UserManager<User> _userManager;

    public AdminService(AppDbContext context, IMapper mapper, LicenseService licenseService, RoleManager<IdentityRole<Guid>> roleManager, UserManager<User> userManager)
    {
        _context = context;
        _mapper = mapper;
        _licenseService = licenseService;
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

    public async Task<(List<AdminRestaurantDto> Restaurants, int TotalCount)> GetRestaurantsByUserIdAsync(
        Guid userId,
        int pageNumber = 1, 
        int pageSize = 20,
        string? searchKey = null,
        string? city = null,
        string? district = null,
        string? neighbourhood = null,
        bool? active = null)
        //bool? hasLicense = null,
        //bool? inPersonOrder = null,
        //bool? onlineOrder = null,
    {
        var query = _context.Restaurants
            .AsNoTracking()
            .Include(r => r.User)
            .Include(r => r.License)
            .Include(r => r.Categories)
            .ThenInclude(c => c.Products)
            .Include(r => r.Products)
            .Include(r => r.User!.Dealer)
            .Where(r => r.UserId == userId) // Filter by specific user
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(searchKey))
        {
            query = query.Where(r => 
                r.Name.Contains(searchKey) ||
                r.City.Contains(searchKey) ||
                r.District.Contains(searchKey) ||
                (r.Neighbourhood != null && r.Neighbourhood.Contains(searchKey)) ||
                r.Telefon.Contains(searchKey)
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

       /*
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

        if (inPersonOrder.HasValue)
        {
            query = query.Where(r => r.InPersonOrder == inPersonOrder.Value);
        }

        if (onlineOrder.HasValue)
        {
            query = query.Where(r => r.OnlineOrder == onlineOrder.Value);
        }*/

        var totalCount = await query.CountAsync();

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

        return (restaurants, totalCount);
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

    #region Dashboard Stats

    public async Task<AdminDashboardStatsDto> GetDashboardStatsAsync()
    {
        var now = DateTime.UtcNow;
        var thisMonth = new DateTime(now.Year, now.Month, 1);

        var users = await _userManager.Users.AsNoTracking().ToListAsync();
        var restaurants = await _context.Restaurants.AsNoTracking().ToListAsync();
        var licenses = await _context.Licenses.AsNoTracking().ToListAsync();
        var orders = await _context.Orders.AsNoTracking().ToListAsync();

        return new AdminDashboardStatsDto
        {
            TotalUsers = users.Count,
            ActiveUsers = users.Count(u => u.IsActive),
            VerifiedUsers = users.Count(u => u.EmailConfirmed),
            NewUsersThisMonth = users.Count(u => u.CreatedAt >= thisMonth),
            
            TotalRestaurants = restaurants.Count,
            ActiveRestaurants = restaurants.Count(r => r.IsActive),
            RestaurantsWithLicense = restaurants.Count(r => r.LicenseId.HasValue),
            NewRestaurantsThisMonth = restaurants.Count(r => r.CreatedAt >= thisMonth),
            
            TotalLicenses = licenses.Count,
            ActiveLicenses = licenses.Count(l => l.IsActive && l.EndDateTime > now),
            ExpiredLicenses = licenses.Count(l => l.EndDateTime <= now),
            ExpiringThisWeek = licenses.Count(l => l.EndDateTime > now && l.EndDateTime <= now.AddDays(7)),
            
            TotalOrders = orders.Count,
            OrdersThisMonth = orders.Count(o => o.CreatedAt >= thisMonth),
            PendingOrders = orders.Count(o => o.Status == OrderStatus.Pending),
            CompletedOrders = orders.Count(o => o.Status == OrderStatus.Completed)
        };
    }

    #endregion

    #region Analytics Methods

    public async Task<object> GetUserRoleStatsAsync()
    {
        var users = await _userManager.Users.AsNoTracking().ToListAsync();
        var thisMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        return new
        {
            Total = new
            {
                Owners = users.Count(u => u.Role == UserRole.Owner),
                Dealers = users.Count(u => u.Role == UserRole.Dealer),
                Customers = users.Count(u => u.Role == UserRole.Customer),
                Managers = users.Count(u => u.Role == UserRole.Manager)
            },
            Active = new
            {
                Owners = users.Count(u => u.Role == UserRole.Owner && u.IsActive),
                Dealers = users.Count(u => u.Role == UserRole.Dealer && u.IsActive),
                Customers = users.Count(u => u.Role == UserRole.Customer && u.IsActive),
                Managers = users.Count(u => u.Role == UserRole.Manager && u.IsActive)
            },
            NewThisMonth = new
            {
                Owners = users.Count(u => u.Role == UserRole.Owner && u.CreatedAt >= thisMonth),
                Dealers = users.Count(u => u.Role == UserRole.Dealer && u.CreatedAt >= thisMonth),
                Customers = users.Count(u => u.Role == UserRole.Customer && u.CreatedAt >= thisMonth),
                Managers = users.Count(u => u.Role == UserRole.Manager && u.CreatedAt >= thisMonth)
            }
        };
    }

    public async Task<object> GetRestaurantAnalyticsAsync()
    {
        var restaurants = await _context.Restaurants
            .Include(r => r.Categories)
            .ThenInclude(c => c.Products)
            .AsNoTracking()
            .ToListAsync();

        var thisMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        // Get top cities
        var topCities = restaurants
            .GroupBy(r => r.City)
            .Select(g => new { City = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToList();

        // Calculate average products per restaurant
        var restaurantsWithProducts = restaurants.Where(r => r.Categories?.Any() == true).ToList();
        var averageProductsPerRestaurant = restaurantsWithProducts.Any() 
            ? restaurantsWithProducts.Average(r => r.Categories?.Sum(c => c.Products?.Count ?? 0) ?? 0)
            : 0;

        return new
        {
            TopCities = topCities,
            AverageProductsPerRestaurant = Math.Round(averageProductsPerRestaurant, 2),
            RestaurantsByCity = restaurants
                .GroupBy(r => r.City)
                .Select(g => new { City = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList(),
            NewThisMonth = restaurants.Count(r => r.CreatedAt >= thisMonth)
        };
    }

    public async Task<object> GetFinancialAnalyticsAsync()
    {
        var licenses = await _context.Licenses.AsNoTracking().ToListAsync();
        var orders = await _context.Orders.AsNoTracking().ToListAsync();
        
        var thisMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        // License revenue
        var licenseRevenue = licenses
            .Where(l => l.IsActive && l.UserPrice.HasValue)
            .Sum(l => l.UserPrice.Value);

        var monthlyLicenseRevenue = licenses
            .Where(l => l.IsActive && l.UserPrice.HasValue && l.StartDateTime >= thisMonth)
            .Sum(l => l.UserPrice.Value);

        // Order revenue (placeholder - would need order pricing)
        var orderRevenue = 0.0m; // TODO: Calculate from order totals
        var monthlyOrderRevenue = 0.0m; // TODO: Calculate from order totals

        return new
        {
            LicenseRevenue = new
            {
                Total = licenseRevenue,
                Monthly = monthlyLicenseRevenue
            },
            OrderRevenue = new
            {
                Total = orderRevenue,
                Monthly = monthlyOrderRevenue
            },
            TotalSystemRevenue = (decimal)licenseRevenue + orderRevenue,
            MonthlySystemRevenue = (decimal)monthlyLicenseRevenue + monthlyOrderRevenue
        };
    }

    #endregion

    #region Utility Methods 

    public async Task<List<AdminUserSummaryDto>> GetAvailableOwnersAsync()
    {
        var owners = await _userManager.Users
            .Where(u => u.Role == UserRole.Owner && u.IsActive)
            .Select(u => new AdminUserSummaryDto 
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                FullName = u.FirstName + " " + u.LastName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                Role = u.Role.ToString(),
                IsActive = u.IsActive,
                IsDealer = u.IsDealer,
                City = u.City,
                District = u.District
            }).OrderBy(u => u.FullName)
            .ToListAsync();

        return owners;
    }

    public async Task<List<AdminUserSummaryDto>> GetAvailableDealersAsync()
    {
        var dealers = await _userManager.Users
            .Where(u => u.Role == UserRole.Dealer && u.IsActive)
            .Select(u => new AdminUserSummaryDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                FullName = u.FirstName + " " + u.LastName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                Role = u.Role.ToString(),
                IsActive = u.IsActive,
                IsDealer = u.IsDealer,
                City = u.City,
                District = u.District
            }).OrderBy(u => u.FullName)
            .ToListAsync();

        return dealers;
    }

    public async Task<List<string>> GetDistinctCitiesAsync()
    {
        var cities = await _context.Restaurants
            .Where(r => !string.IsNullOrEmpty(r.City))
            .Select(r => r.City)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        return cities;
    }

    #endregion

    #region Dealer Management

    public async Task<(List<AdminUserDto> Dealers, int TotalCount)> GetDealersAsync(
        string? search = null,
        bool? isActive = null,
        bool? hasLicenses = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = _userManager.Users
            .Where(u => u.Role == UserRole.Dealer)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u => 
                u.FirstName.Contains(search) ||
                u.LastName.Contains(search) ||
                u.Email.Contains(search) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(search))
            );
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        if (hasLicenses.HasValue)
        {
            if (hasLicenses.Value)
                query = query.Where(u => u.Licenses != null && u.Licenses.Any());
            else
                query = query.Where(u => u.Licenses == null || !u.Licenses.Any());
        }

        var total = await query.CountAsync();

        var dealers = await query
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Skip((page - 1) * pageSize)
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
                AccessFailedCount = u.AccessFailedCount,
                LockoutEnd = u.LockoutEnd,
                RestaurantsCount = u.Restaurants != null ? u.Restaurants.Count : 0,
                LicensesCount = u.Licenses != null ? u.Licenses.Count : 0,
                ActiveRestaurantsCount = u.Restaurants != null ? u.Restaurants.Count(r => r.IsActive) : 0,
                ActiveLicensesCount = u.Licenses != null ? u.Licenses.Count(l => l.IsActive) : 0
            }).ToListAsync();

        return (dealers, total);
    }

    public async Task<AdminDealerDetailDto?> GetDealerDetailAsync(Guid dealerId)
    {
        var dealer = await _userManager.Users
            .Include(u => u.Licenses)
            .ThenInclude(l => l.Restaurant)
            .ThenInclude(r => r.User)
            .Include(u => u.Restaurants)
            .ThenInclude(r => r.Categories)
            .FirstOrDefaultAsync(u => u.Id == dealerId && u.Role == UserRole.Dealer);

        if (dealer == null)
            return null;

        var result = new AdminDealerDetailDto
        {
            Id = dealer.Id,
            FirstName = dealer.FirstName,
            LastName = dealer.LastName,
            Email = dealer.Email,
            PhoneNumber = dealer.PhoneNumber,
            Role = dealer.Role.ToString(),
            IsActive = dealer.IsActive,
            IsDealer = dealer.IsDealer,
            EmailConfirmed = dealer.EmailConfirmed,
            City = dealer.City,
            District = dealer.District,
            Neighbourhood = dealer.Neighbourhood,
            DealerId = dealer.DealerId,
            AccessFailedCount = dealer.AccessFailedCount,
            LockoutEnd = dealer.LockoutEnd,
            RestaurantsCount = dealer.Restaurants?.Count ?? 0,
            LicensesCount = dealer.Licenses?.Count ?? 0,
            ActiveRestaurantsCount = dealer.Restaurants?.Count(r => r.IsActive) ?? 0,
            ActiveLicensesCount = dealer.Licenses?.Count(l => l.IsActive) ?? 0
        };

        // Get licenses
        if (dealer.Licenses != null)
        {
            result.Licenses = dealer.Licenses.Select(l => new AdminLicenseDto
            {
                Id = l.Id,
                StartDateTime = l.StartDateTime,
                EndDateTime = l.EndDateTime,
                IsActive = l.IsActive,
                UserPrice = l.UserPrice,
                DealerPrice = l.DealerPrice,
                UserId = l.UserId,
                UserName = l.User?.FirstName + " " + l.User?.LastName,
                UserEmail = l.User?.Email,
                UserPhone = l.User?.PhoneNumber,
                UserIsActive = l.User?.IsActive ?? false,
                UserIsDealer = l.User?.IsDealer ?? false,
                RestaurantId = l.RestaurantId,
                RestaurantName = l.Restaurant?.Name,
                RestaurantCity = l.Restaurant?.City,
                RestaurantDistrict = l.Restaurant?.District,
                RestaurantIsActive = l.Restaurant?.IsActive ?? false,
                RestaurantOwnerName = l.Restaurant?.User?.FirstName + " " + l.Restaurant?.User?.LastName,
                RestaurantOwnerEmail = l.Restaurant?.User?.Email
            }).ToList();
        }

        // Get managed restaurants (restaurants assigned to this dealer)
        if (dealer.Restaurants != null)
        {
            result.ManagedRestaurants = dealer.Restaurants.Select(r => new AdminRestaurantSummaryDto
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

        // Calculate stats
        result.Stats = new DealerStatsDto
        {
            TotalLicenses = dealer.Licenses?.Count ?? 0,
            ActiveLicenses = dealer.Licenses?.Count(l => l.IsActive) ?? 0,
            ExpiredLicenses = dealer.Licenses?.Count(l => l.EndDateTime <= DateTime.UtcNow) ?? 0,
            ManagedRestaurants = dealer.Restaurants?.Count ?? 0,
            ActiveRestaurants = dealer.Restaurants?.Count(r => r.IsActive) ?? 0,
            TotalRevenue = 0, // Calculate from orders
            MonthlyRevenue = 0, // Calculate from orders
            TotalCustomers = 0, // Calculate from unique customers
            LastActivity = null // Calculate from last login or activity
        };

        return result;
    }

    public async Task<AdminUserDto?> CreateDealerAsync(CreateDealerDto dto)
    {
        var dealer = new User
        {
            Id = Guid.NewGuid(),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            UserName = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            Role = UserRole.Dealer,
            IsActive = dto.IsActive,
            IsDealer = true,
            EmailConfirmed = dto.EmailConfirmed,
            City = dto.City,
            District = dto.District,
            Neighbourhood = dto.Neighbourhood,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        var result = await _userManager.CreateAsync(dealer, dto.Password);
        if (!result.Succeeded)
            return null;

        return new AdminUserDto
        {
            Id = dealer.Id,
            FirstName = dealer.FirstName,
            LastName = dealer.LastName,
            Email = dealer.Email,
            PhoneNumber = dealer.PhoneNumber,
            Role = dealer.Role.ToString(),
            IsActive = dealer.IsActive,
            IsDealer = dealer.IsDealer,
            EmailConfirmed = dealer.EmailConfirmed,
            City = dealer.City,
            District = dealer.District,
            Neighbourhood = dealer.Neighbourhood,
            RestaurantsCount = 0,
            LicensesCount = 0,
            ActiveRestaurantsCount = 0,
            ActiveLicensesCount = 0
        };
    }

    public async Task<bool> UpdateDealerAsync(Guid dealerId, UpdateDealerDto dto)
    {
        var dealer = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == dealerId && u.Role == UserRole.Dealer);
        if (dealer == null) return false;

        if (dto.FirstName != null) dealer.FirstName = dto.FirstName;
        if (dto.LastName != null) dealer.LastName = dto.LastName;
        if (dto.Email != null) dealer.Email = dto.Email;
        if (dto.PhoneNumber != null) dealer.PhoneNumber = dto.PhoneNumber;
        if (dto.City != null) dealer.City = dto.City;
        if (dto.District != null) dealer.District = dto.District;
        if (dto.Neighbourhood != null) dealer.Neighbourhood = dto.Neighbourhood;
        if (dto.IsActive.HasValue) dealer.IsActive = dto.IsActive.Value;
        if (dto.EmailConfirmed.HasValue) dealer.EmailConfirmed = dto.EmailConfirmed.Value;

        var result = await _userManager.UpdateAsync(dealer);
        return result.Succeeded;
    }

    public async Task<bool> AssignRestaurantsToDealerAsync(Guid dealerId, List<Guid> restaurantIds)
    {
        var dealer = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == dealerId && u.Role == UserRole.Dealer);
        if (dealer == null) return false;

        var restaurants = await _context.Restaurants
            .Where(r => restaurantIds.Contains(r.Id))
            .ToListAsync();

        foreach (var restaurant in restaurants)
        {
            restaurant.DealerId = dealerId;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<DealerStatsDto?> GetDealerStatsAsync(Guid dealerId)
    {
        var dealer = await _userManager.Users
            .Include(u => u.Licenses)
            .Include(u => u.Restaurants)
            .FirstOrDefaultAsync(u => u.Id == dealerId && u.Role == UserRole.Dealer);

        if (dealer == null) return null;

        return new DealerStatsDto
        {
            TotalLicenses = dealer.Licenses?.Count ?? 0,
            ActiveLicenses = dealer.Licenses?.Count(l => l.IsActive) ?? 0,
            ExpiredLicenses = dealer.Licenses?.Count(l => l.EndDateTime <= DateTime.UtcNow) ?? 0,
            ManagedRestaurants = dealer.Restaurants?.Count ?? 0,
            ActiveRestaurants = dealer.Restaurants?.Count(r => r.IsActive) ?? 0,
            TotalRevenue = 0, // Calculate from orders
            MonthlyRevenue = 0, // Calculate from orders
            TotalCustomers = 0, // Calculate from unique customers
            LastActivity = null // Calculate from last login or activity
        };
    }

    public async Task<object> BulkUpdateDealerStatusAsync(BulkStatusUpdateDto dto)
    {
        var dealers = await _userManager.Users
            .Where(u => dto.Ids.Contains(u.Id) && u.Role == UserRole.Dealer)
            .ToListAsync();

        var successCount = 0;
        foreach (var dealer in dealers)
        {
            dealer.IsActive = dto.IsActive ?? true;
            var result = await _userManager.UpdateAsync(dealer);
            if (result.Succeeded) successCount++;
        }

        return new { 
            message = $"{successCount} dealers updated successfully", 
            successCount, 
            totalRequested = dto.Ids.Count 
        };
    }

    #endregion

    #region Owner Management

    public async Task<(List<AdminUserDto> Owners, int TotalCount)> GetOwnersAsync(
        string? search = null,
        bool? isActive = null,
        bool? hasRestaurants = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = _userManager.Users
            .Where(u => u.Role == UserRole.Owner)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u => 
                u.FirstName.Contains(search) ||
                u.LastName.Contains(search) ||
                u.Email.Contains(search) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(search))
            );
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        if (hasRestaurants.HasValue)
        {
            if (hasRestaurants.Value)
                query = query.Where(u => u.Restaurants != null && u.Restaurants.Any());
            else
                query = query.Where(u => u.Restaurants == null || !u.Restaurants.Any());
        }

        var total = await query.CountAsync();

        var owners = await query
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Skip((page - 1) * pageSize)
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
                AccessFailedCount = u.AccessFailedCount,
                LockoutEnd = u.LockoutEnd,
                RestaurantsCount = u.Restaurants != null ? u.Restaurants.Count : 0,
                LicensesCount = u.Licenses != null ? u.Licenses.Count : 0,
                ActiveRestaurantsCount = u.Restaurants != null ? u.Restaurants.Count(r => r.IsActive) : 0,
                ActiveLicensesCount = u.Licenses != null ? u.Licenses.Count(l => l.IsActive) : 0
            }).ToListAsync();

        return (owners, total);
    }

    public async Task<AdminOwnerDetailDto?> GetOwnerDetailAsync(Guid ownerId)
    {
        var owner = await _userManager.Users
            .Include(u => u.Restaurants)
            .ThenInclude(r => r.Categories)
            .Include(u => u.Licenses)
            .ThenInclude(l => l.Restaurant)
            .Include(u => u.Dealer)
            .FirstOrDefaultAsync(u => u.Id == ownerId && u.Role == UserRole.Owner);

        if (owner == null)
            return null;

        var result = new AdminOwnerDetailDto
        {
            Id = owner.Id,
            FirstName = owner.FirstName,
            LastName = owner.LastName,
            Email = owner.Email,
            PhoneNumber = owner.PhoneNumber,
            Role = owner.Role.ToString(),
            IsActive = owner.IsActive,
            IsDealer = owner.IsDealer,
            EmailConfirmed = owner.EmailConfirmed,
            City = owner.City,
            District = owner.District,
            Neighbourhood = owner.Neighbourhood,
            DealerId = owner.DealerId,
            AccessFailedCount = owner.AccessFailedCount,
            LockoutEnd = owner.LockoutEnd,
            RestaurantsCount = owner.Restaurants?.Count ?? 0,
            LicensesCount = owner.Licenses?.Count ?? 0,
            ActiveRestaurantsCount = owner.Restaurants?.Count(r => r.IsActive) ?? 0,
            ActiveLicensesCount = owner.Licenses?.Count(l => l.IsActive) ?? 0
        };

        // Get restaurants
        if (owner.Restaurants != null)
        {
            result.Restaurants = owner.Restaurants.Select(r => new AdminRestaurantSummaryDto
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

        // Get licenses
        if (owner.Licenses != null)
        {
            result.Licenses = owner.Licenses.Select(l => new AdminLicenseSummaryDto
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

        // Get assigned dealer
        if (owner.Dealer != null)
        {
            result.AssignedDealer = new AdminUserSummaryDto
            {
                Id = owner.Dealer.Id,
                FirstName = owner.Dealer.FirstName,
                LastName = owner.Dealer.LastName,
                FullName = owner.Dealer.FirstName + " " + owner.Dealer.LastName,
                Email = owner.Dealer.Email,
                PhoneNumber = owner.Dealer.PhoneNumber,
                Role = owner.Dealer.Role.ToString(),
                IsActive = owner.Dealer.IsActive,
                IsDealer = owner.Dealer.IsDealer,
                City = owner.Dealer.City,
                District = owner.Dealer.District
            };
        }

        // Calculate stats
        result.Stats = new OwnerStatsDto
        {
            TotalRestaurants = owner.Restaurants?.Count ?? 0,
            ActiveRestaurants = owner.Restaurants?.Count(r => r.IsActive) ?? 0,
            TotalLicenses = owner.Licenses?.Count ?? 0,
            ActiveLicenses = owner.Licenses?.Count(l => l.IsActive) ?? 0,
            TotalProducts = 0, // Calculate from products
            TotalOrders = 0, // Calculate from orders
            MonthlyOrders = 0, // Calculate from orders
            TotalRevenue = 0, // Calculate from orders
            MonthlyRevenue = 0, // Calculate from orders
            LastActivity = null, // Calculate from last login or activity
            HasDealer = owner.DealerId.HasValue
        };

        return result;
    }

    public async Task<AdminUserDto?> CreateOwnerAsync(CreateOwnerDto dto)
    {
        var owner = new User
        {
            Id = Guid.NewGuid(),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            UserName = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            Role = UserRole.Owner,
            IsActive = dto.IsActive,
            IsDealer = false,
            EmailConfirmed = dto.EmailConfirmed,
            City = dto.City,
            District = dto.District,
            Neighbourhood = dto.Neighbourhood,
            DealerId = dto.DealerId,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        var result = await _userManager.CreateAsync(owner, dto.Password);
        if (!result.Succeeded)
            return null;

        return new AdminUserDto
        {
            Id = owner.Id,
            FirstName = owner.FirstName,
            LastName = owner.LastName,
            Email = owner.Email,
            PhoneNumber = owner.PhoneNumber,
            Role = owner.Role.ToString(),
            IsActive = owner.IsActive,
            IsDealer = owner.IsDealer,
            EmailConfirmed = owner.EmailConfirmed,
            City = owner.City,
            District = owner.District,
            Neighbourhood = owner.Neighbourhood,
            DealerId = owner.DealerId,
            RestaurantsCount = 0,
            LicensesCount = 0,
            ActiveRestaurantsCount = 0,
            ActiveLicensesCount = 0
        };
    }

    public async Task<bool> UpdateOwnerAsync(Guid ownerId, UpdateOwnerDto dto)
    {
        var owner = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == ownerId && u.Role == UserRole.Owner);
        if (owner == null) return false;

        if (dto.FirstName != null) owner.FirstName = dto.FirstName;
        if (dto.LastName != null) owner.LastName = dto.LastName;
        if (dto.Email != null) owner.Email = dto.Email;
        if (dto.PhoneNumber != null) owner.PhoneNumber = dto.PhoneNumber;
        if (dto.City != null) owner.City = dto.City;
        if (dto.District != null) owner.District = dto.District;
        if (dto.Neighbourhood != null) owner.Neighbourhood = dto.Neighbourhood;
        if (dto.IsActive.HasValue) owner.IsActive = dto.IsActive.Value;
        if (dto.EmailConfirmed.HasValue) owner.EmailConfirmed = dto.EmailConfirmed.Value;
        if (dto.DealerId.HasValue) owner.DealerId = dto.DealerId.Value;

        var result = await _userManager.UpdateAsync(owner);
        return result.Succeeded;
    }

    public async Task<bool> AssignDealerToOwnerAsync(Guid ownerId, Guid dealerId)
    {
        var owner = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == ownerId && u.Role == UserRole.Owner);
        var dealer = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == dealerId && u.Role == UserRole.Dealer);

        if (owner == null || dealer == null) return false;

        owner.DealerId = dealerId;
        var result = await _userManager.UpdateAsync(owner);
        return result.Succeeded;
    }

    public async Task<OwnerStatsDto?> GetOwnerStatsAsync(Guid ownerId)
    {
        var owner = await _userManager.Users
            .Include(u => u.Restaurants)
            .Include(u => u.Licenses)
            .FirstOrDefaultAsync(u => u.Id == ownerId && u.Role == UserRole.Owner);

        if (owner == null) return null;

        return new OwnerStatsDto
        {
            TotalRestaurants = owner.Restaurants?.Count ?? 0,
            ActiveRestaurants = owner.Restaurants?.Count(r => r.IsActive) ?? 0,
            TotalLicenses = owner.Licenses?.Count ?? 0,
            ActiveLicenses = owner.Licenses?.Count(l => l.IsActive) ?? 0,
            TotalProducts = 0, // Calculate from products
            TotalOrders = 0, // Calculate from orders
            MonthlyOrders = 0, // Calculate from orders
            TotalRevenue = 0, // Calculate from orders
            MonthlyRevenue = 0, // Calculate from orders
            LastActivity = null, // Calculate from last login or activity
            HasDealer = owner.DealerId.HasValue
        };
    }

    public async Task<object> BulkUpdateOwnerStatusAsync(BulkStatusUpdateDto dto)
    {
        var owners = await _userManager.Users
            .Where(u => dto.Ids.Contains(u.Id) && u.Role == UserRole.Owner)
            .ToListAsync();

        var successCount = 0;
        foreach (var owner in owners)
        {
            owner.IsActive = dto.IsActive ?? true;
            var result = await _userManager.UpdateAsync(owner);
            if (result.Succeeded) successCount++;
        }

        return new { 
            message = $"{successCount} owners updated successfully", 
            successCount, 
            totalRequested = dto.Ids.Count 
        };
    }

    #endregion
}


