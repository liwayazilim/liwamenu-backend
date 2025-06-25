using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RestaurantSystem.Application.Restaurants.DTOs;
using RestaurantSystem.Domain;
using RestaurantSystem.Infrastructure;

namespace RestaurantSystem.Application.Restaurants;

public class RestaurantService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public RestaurantService(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<(List<RestaurantReadDto> Restaurants, int TotalCount)> GetAllAsync(string? search = null, string? city = null, bool? isActive = null, int page = 1, int pageSize = 20)
    {
        var query = _context.Restaurants.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(r => r.Name.Contains(search) || r.Address.Contains(search) || r.City.Contains(search));
        }
        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(r => r.City == city);
        }
        if (isActive.HasValue)
        {
            query = query.Where(r => r.IsActive == isActive.Value);
        }
        var total = await query.CountAsync();
        var restaurants = await query.OrderBy(r => r.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (_mapper.Map<List<RestaurantReadDto>>(restaurants), total);
    }

    public async Task<RestaurantReadDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.Restaurants.FindAsync(id);
        return entity == null ? null : _mapper.Map<RestaurantReadDto>(entity);
    }

    public async Task<RestaurantReadDto> CreateAsync(RestaurantCreateDto dto)
    {
        var entity = _mapper.Map<Restaurant>(dto);
        entity.Id = Guid.NewGuid();
        _context.Restaurants.Add(entity);
        await _context.SaveChangesAsync();
        return _mapper.Map<RestaurantReadDto>(entity);
    }

    public async Task<bool> UpdateAsync(Guid id, RestaurantUpdateDto dto)
    {
        var entity = await _context.Restaurants.FindAsync(id);
        if (entity == null) return false;
        _mapper.Map(dto, entity);
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