using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QR_Menu.Application.Categories.DTOs;
using QR_Menu.Domain;
using QR_Menu.Infrastructure;

namespace QR_Menu.Application.Categories;

public class CategoriesService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<CategoriesService> _logger;

    public CategoriesService(AppDbContext context, IMapper mapper, ILogger<CategoriesService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<(List<CategoryReadDto> Categories, int Total)> GetByRestaurantAsync(Guid restaurantId, string? search, bool? active, int page = 1, int pageSize = 20)
    {
        var q = _context.Categories
            .AsNoTracking()
            .Include(c => c.Products)
            .Where(c => c.RestaurantId == restaurantId);
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(c => c.Name.Contains(search));
        if (active.HasValue) q = q.Where(c => c.IsActive == active.Value);
        var total = await q.CountAsync();
        var list = await q
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (_mapper.Map<List<CategoryReadDto>>(list), total);
    }

    public async Task<CategoryReadDto?> GetByIdAsync(Guid id)
    {
        var category = await _context.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);
        return category == null ? null : _mapper.Map<CategoryReadDto>(category);
    }

    public async Task<(CategoryReadDto? Category, string? Error)> CreateAsync(CategoryCreateDto dto)
    {
        var restaurantExists = await _context.Restaurants.AnyAsync(r => r.Id == dto.RestaurantId);
        if (!restaurantExists) return (null, "Restoran bulunamadı.");
        
        var category = _mapper.Map<Category>(dto);
        category.Id = Guid.NewGuid();
        
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        
        category = await _context.Categories.Include(c => c.Products).FirstAsync(c => c.Id == category.Id);
        return (_mapper.Map<CategoryReadDto>(category), null);
    }

    public async Task<(List<CategoryReadDto> Categories, string? Error)> BulkCreateAsync(BulkCategoryCreateDto dto)
    {
        var restaurantExists = await _context.Restaurants.AnyAsync(r => r.Id == dto.RestaurantId);
        if (!restaurantExists) return (new List<CategoryReadDto>(), "Restoran bulunamadı.");

        var createdCategories = new List<CategoryReadDto>();
        var errors = new List<string>();

        foreach (var categoryDto in dto.Categories)
        {
            categoryDto.RestaurantId = dto.RestaurantId; // Ensure restaurant ID is set
            
            var (category, error) = await CreateAsync(categoryDto);
            if (category != null)
                createdCategories.Add(category);
            else if (!string.IsNullOrEmpty(error))
                errors.Add($"'{categoryDto.Name}': {error}");
        }

        if (errors.Any())
        {
            var errorMessage = string.Join("; ", errors);
            _logger.LogWarning("Bulk create categories completed with errors: {Errors}", errorMessage);
        }

        return (createdCategories, errors.Any() ? string.Join("; ", errors) : null);
    }

    public async Task<bool> UpdateAsync(Guid id, CategoryUpdateDto dto)
    {
        var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id);
        if (category == null) return false;
        
        // Update only the fields that are provided (not null)
        if (!string.IsNullOrWhiteSpace(dto.Name)) 
            category.Name = dto.Name;
        
        if (dto.Icon != null) 
            category.Icon = dto.Icon;
        
        category.LastUpdateDateTime = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<(bool Success, string? Error)> BulkUpdateAsync(BulkCategoryUpdateDto dto)
    {
        var successCount = 0;
        var errors = new List<string>();

        foreach (var categoryDto in dto.Categories)
        {
            var success = await UpdateAsync(categoryDto.Id, new CategoryUpdateDto
            {
                Name = categoryDto.Name,
                Icon = categoryDto.Icon
            });

            if (success)
                successCount++;
            else
                errors.Add($"Kategori ID {categoryDto.Id} güncellenemedi.");
        }

        if (errors.Any())
        {
            var errorMessage = string.Join("; ", errors);
            _logger.LogWarning("Bulk update categories completed with errors: {Errors}", errorMessage);
        }

        return (successCount > 0, errors.Any() ? string.Join("; ", errors) : null);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var category = await _context.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);
        if (category == null) return false;
        if (category.Products != null && category.Products.Any())
            return false; // avoid deleting categories with products for now
        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
        return true;
    }
} 