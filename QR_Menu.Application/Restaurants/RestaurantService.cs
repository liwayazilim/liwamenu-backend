using AutoMapper;
using Microsoft.EntityFrameworkCore;
using QR_Menu.Application.Restaurants.DTOs;
using QR_Menu.Domain;
using QR_Menu.Infrastructure;

namespace QR_Menu.Application.Restaurants;

public class RestaurantService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public RestaurantService(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
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
            query = query.Where(r => r.City == city);
        }
        if (active.HasValue)
        {
            query = query.Where(r => r.IsActive == active.Value);
        }
        if (!string.IsNullOrWhiteSpace(district))
        {
            query = query.Where(r => r.District == district);
        }
        if (!string.IsNullOrWhiteSpace(neighbourhood))
        {
            query = query.Where(r => r.Neighbourhood == neighbourhood);
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