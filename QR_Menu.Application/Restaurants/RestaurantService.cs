using AutoMapper;
using Microsoft.EntityFrameworkCore;
using QR_Menu.Application.Restaurants.DTOs;
using QR_Menu.Domain;
using QR_Menu.Infrastructure;
using Microsoft.AspNetCore.Identity;

namespace QR_Menu.Application.Restaurants;

public class RestaurantService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly UserManager<User> _userManager;

    public RestaurantService(AppDbContext context, IMapper mapper, UserManager<User> userManager)
    {
        _context = context;
        _mapper = mapper;
        _userManager = userManager;
    }

    public async Task<(List<RestaurantReadDto> Restaurants, int TotalCount)> GetAllAsync(string? searchKey = null, string? city = null, bool? active = null, int pageNumber = 1, int pageSize = 10, string? district = null, string? neighbourhood = null)
    {
        var query = _context.Restaurants.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(searchKey))
        {
            query = query.Where(r => r.Name.Contains(searchKey) || r.Address.Contains(searchKey) || r.City.Contains(searchKey));
        }
        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(r => r.City.ToLower() == city.ToLower());
        }
        if (active.HasValue)
        {
            query = query.Where(r => r.IsActive == active.Value);
        }
        if (!string.IsNullOrWhiteSpace(district))
        {
            query = query.Where(r => r.District.ToLower() == district.ToLower());
        }
        if (!string.IsNullOrWhiteSpace(neighbourhood))
        {
            query = query.Where(r => r.Neighbourhood != null && r.Neighbourhood.ToLower() == neighbourhood.ToLower());
        }
        var total = await query.CountAsync();
        var restaurants = await query.OrderBy(r => r.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (_mapper.Map<List<RestaurantReadDto>>(restaurants), total);
    }

    public async Task<RestaurantReadDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.Restaurants.FindAsync(id);
        return entity == null ? null : _mapper.Map<RestaurantReadDto>(entity);
    }

    public async Task<(RestaurantReadDto? Restaurant, string? ErrorMessage)> CreateAsync(RestaurantCreateDto dto, Guid userId, Guid? dealerId = null)
    {
        // Validate request
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.PhoneNumber) || 
            string.IsNullOrWhiteSpace(dto.City) || string.IsNullOrWhiteSpace(dto.District) ||
            string.IsNullOrWhiteSpace(dto.Address))
        {
            return (null, "Geçersiz istek. Tüm gerekli alanlar doldurulmalıdır.");
        }

        // Check if user exists
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return (null, "Kullanıcı bulunamadı.");
        }

        // Check if dealer exists (if DealerId is provided)
        if (dealerId.HasValue)
        {
            var dealer = await _userManager.FindByIdAsync(dealerId.Value.ToString());
            if (dealer == null || !dealer.IsDealer)
            {
                return (null, "Bayi bulunamadı.");
            }
        }
        else if (user.DealerId.HasValue)
        {
            // Use user's assigned dealer if no dealer specified
            var dealer = await _userManager.FindByIdAsync(user.DealerId.Value.ToString());
            if (dealer == null || !dealer.IsDealer)
            {
                return (null, "Bayi bulunamadı.");
            }
            dealerId = dealer.Id;
        }

        // Create restaurant
        var entity = _mapper.Map<Restaurant>(dto);
        entity.Id = Guid.NewGuid();
        entity.UserId = userId;
        entity.DealerId = dealerId;
        entity.CreatedDateTime = DateTime.UtcNow;
        entity.LastUpdateDateTime = DateTime.UtcNow;
        
        // Map the new field names to the domain model
        entity.Telefon = dto.PhoneNumber; // Map PhoneNumber to Telefon
        entity.Lat = dto.Latitude; // Map Latitude to Lat
        entity.Lng = dto.Longitude; // Map Longitude to Lng
        
        // Set default values for fields not in the simplified DTO
        entity.WorkingHours = null;
        entity.MinDistance = null;
        entity.GoogleAnalytics = null;
        entity.DefaultLang = null;
        entity.InPersonOrder = true;
        entity.OnlineOrder = true;
        entity.Slogan1 = null;
        entity.Slogan2 = null;
        entity.Hide = false;
        
        _context.Restaurants.Add(entity);
        await _context.SaveChangesAsync();

        // Check if this is user's first restaurant and add demo license if needed
        var restaurantCount = await _context.Restaurants.CountAsync(r => r.UserId == userId);
        if (restaurantCount == 1 && !user.IsUseDemoLicense)
        {
            // Add demo license logic here
            // await AddDemoLicensesForMarketplaces(entity.Id, userId);

            // Mark user as having used demo license
            user.IsUseDemoLicense = true;
            await _userManager.UpdateAsync(user);
        }

        return (_mapper.Map<RestaurantReadDto>(entity), null);
    }

    public async Task<bool> UpdateAsync(Guid id, RestaurantUpdateDto dto)
    {
        var entity = await _context.Restaurants.FindAsync(id);
        if (entity == null) return false;
        _mapper.Map(dto, entity);
        entity.LastUpdateDateTime = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _context.Restaurants.FindAsync(id);
        if (entity == null) return false;
        _context.Restaurants.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }
} 