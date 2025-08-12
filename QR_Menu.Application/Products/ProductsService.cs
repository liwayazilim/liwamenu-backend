using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QR_Menu.Application.Products.DTOs;
using QR_Menu.Domain;
using QR_Menu.Infrastructure;

namespace QR_Menu.Application.Products;

public class ProductsService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<ProductsService> _logger;

    public ProductsService(AppDbContext context, IMapper mapper, ILogger<ProductsService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<(List<ProductReadDto> Products, int Total)> GetByRestaurantAsync(Guid restaurantId, Guid? categoryId, string? search, bool? active, int page = 1, int pageSize = 20)
    {
        var q = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.RestaurantId == restaurantId);
        if (categoryId.HasValue) q = q.Where(p => p.CategoryId == categoryId.Value);
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(p => p.Name.Contains(search));
        if (active.HasValue) q = q.Where(p => p.IsActive == active.Value);
        var total = await q.CountAsync();
        var list = await q
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (_mapper.Map<List<ProductReadDto>>(list), total);
    }

    public async Task<ProductReadDto?> GetByIdAsync(Guid id)
    {
        var product = await _context.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
        return product == null ? null : _mapper.Map<ProductReadDto>(product);
    }

    public async Task<(ProductReadDto? Product, string? Error)> CreateAsync(ProductCreateDto dto)
    {
        var restaurant = await _context.Restaurants.AnyAsync(r => r.Id == dto.RestaurantId);
        if (!restaurant) return (null, "Restoran bulunamadı.");
        var category = await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId && c.RestaurantId == dto.RestaurantId);
        if (!category) return (null, "Kategori bulunamadı veya bu restorana ait değil.");

        var product = new Product
        {
            Id = Guid.NewGuid(),
            RestaurantId = dto.RestaurantId,
            CategoryId = dto.CategoryId,
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            IsActive = dto.IsActive
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        product = await _context.Products.Include(p => p.Category).FirstAsync(p => p.Id == product.Id);
        return (_mapper.Map<ProductReadDto>(product), null);
    }

    public async Task<bool> UpdateAsync(Guid id, ProductUpdateDto dto)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return false;
        if (dto.CategoryId.HasValue)
        {
            var belongs = await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId.Value && c.RestaurantId == product.RestaurantId);
            if (!belongs) return false;
            product.CategoryId = dto.CategoryId.Value;
        }
        if (!string.IsNullOrWhiteSpace(dto.Name)) product.Name = dto.Name;
        if (dto.Description != null) product.Description = dto.Description;
        if (dto.Price.HasValue) product.Price = dto.Price.Value;
        if (dto.IsActive.HasValue) product.IsActive = dto.IsActive.Value;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return false;
        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return true;
    }
} 