using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QR_Menu.Application.OrderTags;
using QR_Menu.Domain;
using QR_Menu.Infrastructure;

namespace QR_Menu.Application.OrderTags;

public class OrderTagsService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<OrderTagsService> _logger;

    public OrderTagsService(AppDbContext context, IMapper mapper, ILogger<OrderTagsService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<(List<OrderTagReadDto> Tags, int Total)> GetByRestaurantAsync(
        Guid restaurantId, 
        string? search = null, 
        int page = 1, 
        int pageSize = 20)
    {
        var query = _context.OrderTags
            .AsNoTracking()
            .Where(ot => ot.RestaurantId == restaurantId);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(ot => ot.Name.Contains(search));

        var total = await query.CountAsync();
        var list = await query
            .OrderBy(ot => ot.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (_mapper.Map<List<OrderTagReadDto>>(list), total);
    }

    public async Task<OrderTagReadDto?> GetByIdAsync(Guid id)
    {
        var tag = await _context.OrderTags
            .FirstOrDefaultAsync(ot => ot.Id == id);
        
        return tag == null ? null : _mapper.Map<OrderTagReadDto>(tag);
    }

    public async Task<(OrderTagReadDto? Tag, string? Error)> CreateAsync(OrderTagCreateDto dto)
    {
        var restaurantExists = await _context.Restaurants
            .AnyAsync(r => r.Id == dto.RestaurantId);
        
        if (!restaurantExists)
            return (null, "Restoran bulunamadı.");

        // Check if tag with same name already exists for this restaurant
        var existingTag = await _context.OrderTags
            .FirstOrDefaultAsync(ot => 
                ot.RestaurantId == dto.RestaurantId && 
                ot.Name.ToLower() == dto.Name.ToLower());
        
        if (existingTag != null)
            return (null, "Bu isimde bir etiket zaten mevcut.");

        var tag = _mapper.Map<OrderTag>(dto);
        tag.Id = Guid.NewGuid();
        tag.CreatedDateTime = DateTime.UtcNow;
        tag.LastUpdateDateTime = DateTime.UtcNow;

        _context.OrderTags.Add(tag);
        await _context.SaveChangesAsync();

        var savedTag = await _context.OrderTags
            .FirstAsync(ot => ot.Id == tag.Id);
        
        return (_mapper.Map<OrderTagReadDto>(savedTag), null);
    }

    public async Task<(List<OrderTagReadDto> Tags, string? Error)> BulkCreateAsync(BulkOrderTagCreateDto dto)
    {
        var restaurantExists = await _context.Restaurants
            .AnyAsync(r => r.Id == dto.RestaurantId);
        
        if (!restaurantExists)
            return (new List<OrderTagReadDto>(), "Restoran bulunamadı.");

        var createdTags = new List<OrderTagReadDto>();
        var errors = new List<string>();

        foreach (var tagDto in dto.Tags)
        {
            tagDto.RestaurantId = dto.RestaurantId; // Ensure restaurant ID is set
            
            var (tag, error) = await CreateAsync(tagDto);
            if (tag != null)
                createdTags.Add(tag);
            else if (!string.IsNullOrEmpty(error))
                errors.Add($"'{tagDto.Name}': {error}");
        }

        if (errors.Any())
        {
            var errorMessage = string.Join("; ", errors);
            _logger.LogWarning("Bulk create order tags completed with errors: {Errors}", errorMessage);
        }

        return (createdTags, errors.Any() ? string.Join("; ", errors) : null);
    }

    public async Task<bool> UpdateAsync(Guid id, OrderTagUpdateDto dto)
    {
        var tag = await _context.OrderTags
            .FirstOrDefaultAsync(ot => ot.Id == id);
        
        if (tag == null) return false;

        // Check if name change would conflict with existing tag
        if (!string.IsNullOrWhiteSpace(dto.Name) && dto.Name != tag.Name)
        {
            var existingTag = await _context.OrderTags
                .FirstOrDefaultAsync(ot => 
                    ot.RestaurantId == tag.RestaurantId && 
                    ot.Id != tag.Id &&
                    ot.Name.ToLower() == dto.Name.ToLower());
            
            if (existingTag != null) return false; // Name conflict
        }

        // Update only provided fields
        if (!string.IsNullOrWhiteSpace(dto.Name))
            tag.Name = dto.Name;
        
        if (dto.Price.HasValue)
            tag.Price = dto.Price.Value;

        tag.LastUpdateDateTime = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<(bool Success, string? Error)> BulkUpdateAsync(BulkOrderTagUpdateDto dto)
    {
        var successCount = 0;
        var errors = new List<string>();

        foreach (var tagDto in dto.Tags)
        {
            var success = await UpdateAsync(tagDto.Id, new OrderTagUpdateDto
            {
                Name = tagDto.Name,
                Price = tagDto.Price
            });

            if (success)
                successCount++;
            else
                errors.Add($"Etiket ID {tagDto.Id} güncellenemedi.");
        }

        if (errors.Any())
        {
            var errorMessage = string.Join("; ", errors);
            _logger.LogWarning("Bulk update order tags completed with errors: {Errors}", errorMessage);
        }

        return (successCount > 0, errors.Any() ? string.Join("; ", errors) : null);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var tag = await _context.OrderTags
            .Include(ot => ot.Orders)
            .Include(ot => ot.OrderItems)
            .FirstOrDefaultAsync(ot => ot.Id == id);
        
        if (tag == null) return false;

        // Check if tag is being used by any orders or order items
        if ((tag.Orders != null && tag.Orders.Any()) || 
            (tag.OrderItems != null && tag.OrderItems.Any()))
        {
            return false; // Cannot delete tag that is in use
        }

        _context.OrderTags.Remove(tag);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.OrderTags.AnyAsync(ot => ot.Id == id);
    }

    public async Task<bool> ExistsByNameAsync(Guid restaurantId, string name, Guid? excludeId = null)
    {
        var query = _context.OrderTags
            .Where(ot => ot.RestaurantId == restaurantId && 
                        ot.Name.ToLower() == name.ToLower());
        
        if (excludeId.HasValue)
            query = query.Where(ot => ot.Id != excludeId.Value);
        
        return await query.AnyAsync();
    }
} 