using AutoMapper;
using Microsoft.EntityFrameworkCore;
using QR_Menu.Application.Restaurants.DTOs;
using QR_Menu.Domain;
using QR_Menu.Infrastructure;
using Microsoft.AspNetCore.Identity;
using QR_Menu.Application.Common;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace QR_Menu.Application.Restaurants;

public class RestaurantService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly UserManager<User> _userManager;
    private readonly IImageService _imageService;

    public RestaurantService(AppDbContext context, IMapper mapper, UserManager<User> userManager, IImageService imageService)
    {
        _context = context;
        _mapper = mapper;
        _userManager = userManager;
        _imageService = imageService;
    }

    public async Task<(Guid? OwnerId, Guid? DealerId)> GetOwnerAndDealerAsync(Guid restaurantId)
    {
        return await _context.Restaurants
            .AsNoTracking()
            .Where(r => r.Id == restaurantId)
            .Select(r => new ValueTuple<Guid?, Guid?>(r.UserId, r.DealerId))
            .FirstOrDefaultAsync();
    }

    public async Task<WorkingHoursReadDto?> GetWorkingHoursAsync(Guid restaurantId)
    {
        var entity = await _context.Restaurants.AsNoTracking().FirstOrDefaultAsync(r => r.Id == restaurantId);
        if (entity == null) return null;
        var dto = new WorkingHoursReadDto { RestaurantId = restaurantId };
        if (!string.IsNullOrWhiteSpace(entity.WorkingHours))
        {
            try
            {
                dto.Days = JsonSerializer.Deserialize<List<WorkingHoursDayDto>>(entity.WorkingHours!) ?? new();
            }
            catch
            {
                dto.Days = new();
            }
        }
        return dto;
    }

    private static bool IsValidTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        // HH:mm 24h
        return Regex.IsMatch(value, "^(?:[01]\\d|2[0-3]):[0-5]\\d$");
    }

    public async Task<(bool ok, string? error)> SetWorkingHoursAsync(WorkingHoursUpdateDto dto)
    {
        var entity = await _context.Restaurants.FirstOrDefaultAsync(r => r.Id == dto.RestaurantId);
        if (entity == null) return (false, "Restoran bulunamadı.");

        // Basic validations
        if (dto.Days == null || dto.Days.Count == 0) return (false, "Geçersiz çalışma saatleri verisi.");
        var daySet = new HashSet<int>();
        foreach (var d in dto.Days)
        {
            if (d.Day < 1 || d.Day > 7) return (false, "Gün 1-7 aralığında olmalıdır.");
            if (!daySet.Add(d.Day)) return (false, "Aynı gün birden fazla kez gönderilemez.");
            if (!d.IsClosed)
            {
                if (!IsValidTime(d.Open) || !IsValidTime(d.Close)) return (false, "Saat formatı HH:mm olmalıdır.");
                if (TimeOnly.Parse(d.Open!) >= TimeOnly.Parse(d.Close!)) return (false, "Açılış saati kapanıştan önce olmalıdır.");
            }
        }

        entity.WorkingHours = JsonSerializer.Serialize(dto.Days);
        entity.LastUpdateDateTime = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<SocialLinksReadDto?> GetSocialLinksAsync(Guid restaurantId)
    {
        var entity = await _context.Restaurants.AsNoTracking().FirstOrDefaultAsync(r => r.Id == restaurantId);
        if (entity == null) return null ;
        var result = new SocialLinksReadDto { RestaurantId = restaurantId };
        if (!string.IsNullOrWhiteSpace(entity.SocialLinks))
        {
            try
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, string?>>(entity.SocialLinks!) ?? new();
                dict.TryGetValue("facebook", out var fb);
                dict.TryGetValue("instagram", out var ig);
                dict.TryGetValue("tiktok", out var tt);
                dict.TryGetValue("youtube", out var yt);
                dict.TryGetValue("whatsapp", out var wa);
                result.FacebookUrl = fb;
                result.InstagramUrl = ig;
                result.TiktokUrl = tt;
                result.YoutubeUrl = yt;
                result.WhatsappUrl = wa;
            }
            catch
            {
                // ignore malformed json and return empty
            
            }
        }
        return result;
    }

    public async Task<(bool ok, string? error)> SetSocialLinksAsync(SocialLinksUpdateDto dto)
    {
        var entity = await _context.Restaurants.FirstOrDefaultAsync(r => r.Id == dto.RestaurantId);
        if (entity == null) return (false, "Restoran bulunamadı.");

        var dict = new Dictionary<string, string?>
        {
            ["facebook"] = dto.FacebookUrl,
            ["instagram"] = dto.InstagramUrl,
            ["tiktok"] = dto.TiktokUrl,
            ["youtube"] = dto.YoutubeUrl,
            ["whatsapp"] = dto.WhatsappUrl
        };
        entity.SocialLinks = JsonSerializer.Serialize(dict);
        entity.LastUpdateDateTime = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<List<PaymentMethodOptionDto>> GetRestaurantPaymentMethodsAsync(Guid restaurantId)
    {
        // Get all globally active payment methods
        var allMethods = await _context.PaymentMethods
            .AsNoTracking()
            .Where(pm => pm.IsActive)
            .OrderBy(pm => pm.Name)
            .ToListAsync();

        // Get enabled payment methods for this restaurant
        var enabledPaymentMethods = await _context.Restaurants
            .AsNoTracking()
            .Where(r => r.Id == restaurantId)
            .Include(r => r.PaymentMethods)
            .SelectMany(r => r.PaymentMethods!)
            .Select(pm => pm.Id)
            .ToListAsync();

        var enabledIds = enabledPaymentMethods.ToHashSet();

        // Build the result combining all methods with their enabled status for this restaurant
        var result = allMethods.Select(method => new PaymentMethodOptionDto
        {
            Id = method.Id,
            Name = method.Name,
            Enabled = enabledIds.Contains(method.Id)
        }).ToList();

        return result;
    }



    public async Task<(bool ok, string? error)> SetRestaurantPaymentMethodsAsync(PaymentMethodsUpdateDto dto)
    {
        try
        {
            // Get the restaurant with payment methods
            var restaurant = await _context.Restaurants
                .Include(r => r.PaymentMethods)
                .FirstOrDefaultAsync(r => r.Id == dto.RestaurantId);
            
            if (restaurant == null) 
                return (false, "Restoran bulunamadı.");

            // Validate that all provided IDs exist and are globally active
            var validIds = await _context.PaymentMethods
                .Where(pm => pm.IsActive && dto.MethodIds.Contains(pm.Id))
                .Select(pm => pm.Id)
                .ToListAsync();

            if (validIds.Count != dto.MethodIds.Count)
            {
                return (false, "Bazı ödeme yöntemleri geçersiz veya aktif değil.");
            }

            // Get current enabled payment method IDs for this restaurant
            var currentEnabledIds = restaurant.PaymentMethods?.Select(pm => pm.Id).ToHashSet() ?? new HashSet<Guid>();
            
            // Get the payment methods to add
            var paymentMethodsToAdd = await _context.PaymentMethods
                .Where(pm => validIds.Contains(pm.Id) && !currentEnabledIds.Contains(pm.Id))
                .ToListAsync();
            
            // Get the payment methods to remove
            var paymentMethodsToRemove = restaurant.PaymentMethods?
                .Where(pm => !validIds.Contains(pm.Id))
                .ToList() ?? new List<PaymentMethod>();
            
            // Remove payment methods that are no longer enabled
            foreach (var paymentMethod in paymentMethodsToRemove)
            {
                restaurant.PaymentMethods!.Remove(paymentMethod);
            }
            
            // Add new payment methods
            foreach (var paymentMethod in paymentMethodsToAdd)
            {
                restaurant.PaymentMethods ??= new List<PaymentMethod>();
                restaurant.PaymentMethods.Add(paymentMethod);
            }
            
            restaurant.LastUpdateDateTime = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Ödeme yöntemleri güncellenirken hata oluştu: {ex.Message}");
        }
    }

    public async Task<(bool ok, string? error)> AddPaymentMethodToRestaurantAsync(AddPaymentMethodToRestaurantDto dto)
    {
        var restaurant = await _context.Restaurants
            .Include(r => r.PaymentMethods)
            .FirstOrDefaultAsync(r => r.Id == dto.RestaurantId);
        
        if (restaurant == null) 
            return (false, "Restoran bulunamadı.");

        // Check if payment method with this name already exists globally
        var existingPaymentMethod = await _context.PaymentMethods
            .AsNoTracking()
            .FirstOrDefaultAsync(pm => pm.Name.ToLower() == dto.PaymentMethodName.ToLower());

        PaymentMethod paymentMethod;
        
        if (existingPaymentMethod != null)
        {
            // Use existing payment method
            paymentMethod = existingPaymentMethod;
            
            // Check if restaurant already has this payment method
            if (restaurant.PaymentMethods != null && 
                restaurant.PaymentMethods.Any(pm => pm.Id == existingPaymentMethod.Id))
            {
                return (false, "Bu ödeme yöntemi zaten restoranda mevcut.");
            }
        }
        else
        {
            // Create new payment method
            paymentMethod = new PaymentMethod
            {
                Id = Guid.NewGuid(),
                Name = dto.PaymentMethodName,
                IsActive = true,
                CreatedDateTime = DateTime.UtcNow,
                LastUpdateDateTime = DateTime.UtcNow
            };
            
            _context.PaymentMethods.Add(paymentMethod);
        }

        // Add to restaurant
        restaurant.PaymentMethods ??= new List<PaymentMethod>();
        restaurant.PaymentMethods.Add(paymentMethod);
        restaurant.LastUpdateDateTime = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return (true, null);
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

    public async Task<(RestaurantReadDto? Restaurant, string? ErrorMessage)> CreateAsync(RestaurantCreateDto dto, Guid userId, string webRootPath, Guid? dealerId = null)
    {
        // Validate request
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.PhoneNumber) || 
            string.IsNullOrWhiteSpace(dto.City) || string.IsNullOrWhiteSpace(dto.District) ||
            string.IsNullOrWhiteSpace(dto.Address))
        {
            return (null, "Geçersiz istek. Tüm gerekli alanlar doldurulmalıdır.");
        }

        // Validate coordinates
        if (dto.Latitude == 0 && dto.Longitude == 0)
        {
            return (null, "Geçersiz koordinatlar. Lütfen geçerli bir konum seçin.");
        }

        // Validate theme id
        if (dto.ThemeId.HasValue && (dto.ThemeId.Value < 0 || dto.ThemeId.Value > 14))
        {
            return (null, "Geçersiz tema. Tema 0-14 aralığında olmalıdır.");
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
        var entity = new Restaurant
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DealerId = dealerId,
            Name = dto.Name,
            PhoneNumber = dto.PhoneNumber,
            City = dto.City,
            District = dto.District,
            Neighbourhood = dto.Neighbourhood,
            Address = dto.Address,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            IsActive = dto.IsActive,
            ThemeId = dto.ThemeId ?? 0,
            CreatedDateTime = DateTime.UtcNow,
            LastUpdateDateTime = DateTime.UtcNow
        };

        // Process image if provided
        if (dto.Image != null)
        {
            try
            {
                if (!_imageService.IsValidImageFile(dto.Image))
                {
                    return (null, "Geçersiz resim dosyası. Lütfen geçerli bir resim dosyası yükleyin (JPG, PNG, GIF, BMP, maksimum 5MB).");
                }

                // Validate webRootPath
                if (string.IsNullOrEmpty(webRootPath))
                {
                    return (null, "Web root path is null or empty. Please check server configuration.");
                }

                var (imageData, fileName, contentType) = await _imageService.ProcessAndStoreImageAsync(dto.Image, entity.Id, webRootPath);
                entity.ImageData = imageData;
                entity.ImageFileName = fileName;
                entity.ImageContentType = contentType;
            }
            catch (Exception ex)
            {
                return (null, $"Resim işlenirken hata oluştu: {ex.Message}");
            }
        }

        _context.Restaurants.Add(entity);
        await _context.SaveChangesAsync();

        var result = _mapper.Map<RestaurantReadDto>(entity);
        return (result, null);
    }

    public async Task<(bool success, string? errorMessage)> RestaurantTransferAsync(Guid restaurantId, Guid userId)
    {
        // Check if restaurant exists
        var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.Id == restaurantId);
        if (restaurant == null)
            return (false, "Restoran bulunamadı.");

        // Check if user exists
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return (false, "Kullanıcı bulunamadı.");

        // Update restaurant ownership
        restaurant.UserId = userId;
        restaurant.LastUpdateDateTime = DateTime.UtcNow;

        // Update associated licenses
        var licenses = await _context.Licenses
            .Where(l => l.RestaurantId == restaurantId)
            .ToListAsync();

        if (licenses.Any())
        {
            foreach (var license in licenses)
            {
                license.UserId = userId;
                license.LastUpdateDateTime = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(RestaurantReadDto? Restaurant, string? ErrorMessage)> UpdateAsync(Guid id, RestaurantUpdateDto dto, string webRootPath)
    {
        var entity = await _context.Restaurants.FindAsync(dto.RestaurantId);
        if (entity == null) return (null, "Restoran bulunamadı.");
        
        // Update only the fields provided in the DTO (preserve existing values for other fields)
        if (!string.IsNullOrWhiteSpace(dto.Name))
            entity.Name = dto.Name;
        if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
            entity.PhoneNumber = dto.PhoneNumber;
        if (!string.IsNullOrWhiteSpace(dto.City))
            entity.City = dto.City;
        if (!string.IsNullOrWhiteSpace(dto.District))
            entity.District = dto.District;
        if (dto.Neighbourhood != null) // Allow empty string for neighbourhood
            entity.Neighbourhood = dto.Neighbourhood;
        if (!string.IsNullOrWhiteSpace(dto.Address))
            entity.Address = dto.Address;
        
        // Only update coordinates if they are provided and valid
        if (dto.Latitude.HasValue && dto.Latitude.Value != 0)
            entity.Latitude = dto.Latitude.Value;
        if (dto.Longitude.HasValue && dto.Longitude.Value != 0)
            entity.Longitude = dto.Longitude.Value;
        
        // Update theme if provided
        if (dto.ThemeId.HasValue)
        {
            if (dto.ThemeId.Value < 0 || dto.ThemeId.Value > 14)
                return (null, "Geçersiz tema. Tema 0-14 aralığında olmalıdır.");
            entity.ThemeId = dto.ThemeId.Value;
        }
            
        entity.LastUpdateDateTime = DateTime.UtcNow;

        // Process image if provided
        if (dto.Image != null)
        {
            try
            {
                if (!_imageService.IsValidImageFile(dto.Image))
                {
                    return (null, "Geçersiz resim dosyası.");
                }

                // Delete old image if exists
                if (!string.IsNullOrEmpty(entity.ImageFileName))
                {
                    await _imageService.DeleteImageAsync(entity.ImageFileName, webRootPath);
                }

                // Process and store new image
                var (imageData, fileName, contentType) = await _imageService.ProcessAndStoreImageAsync(dto.Image, entity.Id, webRootPath);
                entity.ImageData = imageData;
                entity.ImageFileName = fileName;
                entity.ImageContentType = contentType;
            }
            catch (Exception)
            {
                return (null, "Resim işlenirken hata oluştu.");
            }
        }

        await _context.SaveChangesAsync();
        
        // Return updated restaurant data
        var updatedRestaurant = _mapper.Map<RestaurantReadDto>(entity);
        return (updatedRestaurant, null);
    }

    public async Task<bool> DeleteAsync(Guid id, string webRootPath)
    {
        var entity = await _context.Restaurants.FindAsync(id);
        if (entity == null) return false;
        
        // Delete associated image if exists
        if (!string.IsNullOrEmpty(entity.ImageFileName))
        {
            await _imageService.DeleteImageAsync(entity.ImageFileName, webRootPath);
        }
        
        _context.Restaurants.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<(bool success, string? errorMessage)> DeleteRestaurantByIdAsync(Guid restaurantId, string webRootPath)
    {
        // Check if restaurant exists
        var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.Id == restaurantId);
        if (restaurant == null)
            return (false, "Restoran bulunamadı.");

        // Check if restaurant has licenses
        var restaurantLicenses = await _context.Licenses
            .Where(l => l.RestaurantId == restaurantId)
            .CountAsync();
        
        if (restaurantLicenses > 0)
            return (false, $"Restoranın {restaurantLicenses} ilişkili lisansı var. Restoran silinemez!");

        // Delete associated image if exists
        if (!string.IsNullOrEmpty(restaurant.ImageFileName))
        {
            await _imageService.DeleteImageAsync(restaurant.ImageFileName, webRootPath);
        }

        // If no restrictions, delete the restaurant
        _context.Restaurants.Remove(restaurant);
        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(RestaurantSettingsResponseDto? Settings, string? ErrorMessage)> UpdateSettingsAsync(RestaurantSettingsUpdateDto dto)
    {
        var entity = await _context.Restaurants.FirstOrDefaultAsync(r => r.Id == dto.RestaurantId);
        if (entity == null)
            return (null, "Restoran bulunamadı.");

        // Update only the fields that are provided (not null)
        if (dto.MinDistance.HasValue)
            entity.MinDistance = dto.MinDistance.Value;
        
        if (dto.GoogleAnalytics != null)
            entity.GoogleAnalytics = dto.GoogleAnalytics;
        
        if (dto.DefaultLang != null)
            entity.DefaultLang = dto.DefaultLang;
        
        if (dto.InPersonOrder.HasValue)
            entity.InPersonOrder = dto.InPersonOrder.Value;
        
        if (dto.OnlineOrder.HasValue)
            entity.OnlineOrder = dto.OnlineOrder.Value;
        
        if (dto.Slogan1 != null)
            entity.Slogan1 = dto.Slogan1;
        
        if (dto.Slogan2 != null)
            entity.Slogan2 = dto.Slogan2;
        
        if (dto.Hide.HasValue)
            entity.Hide = dto.Hide.Value;

        // Update timestamp
        entity.LastUpdateDateTime = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Return updated settings
        var responseDto = _mapper.Map<RestaurantSettingsResponseDto>(entity);
        return (responseDto, null);
    }

    public async Task<(RestaurantThemeResponseDto? Theme, string? ErrorMessage)> UpdateThemeAsync(RestaurantThemeUpdateDto dto)
    {
        var entity = await _context.Restaurants.FirstOrDefaultAsync(r => r.Id == dto.RestaurantId);
        if (entity == null)
            return (null, "Restoran bulunamadı.");

        // Validate theme ID
        if (dto.ThemeId < 0 || dto.ThemeId > 14)
        {
            return (null, "Geçersiz tema ID. Tema ID 0-14 aralığında olmalıdır.");
        }

        // Update theme using manual mapping (no AutoMapper)
        entity.ThemeId = dto.ThemeId;
        entity.LastUpdateDateTime = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Return response using manual mapping
        var responseDto = new RestaurantThemeResponseDto
        {
            RestaurantId = entity.Id,
            ThemeId = entity.ThemeId,
            LastUpdateDateTime = entity.LastUpdateDateTime
        };

        return (responseDto, null);
    }
} 