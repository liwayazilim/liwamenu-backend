using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RestaurantSystem.Application.Restaurants.DTOs;
using RestaurantSystem.Domain;
using RestaurantSystem.Domain.Interfaces;

namespace RestaurantSystem.Application.Restaurants;

public class RestaurantService
{
    private readonly IAppDbContext _context;
    private readonly IMapper _mapper;

    public RestaurantService(IAppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<RestaurantDto>> GetAllAsync()
    {
        var restaurants = await _context.Restaurants
            .Include(r => r.Categories)
            .ThenInclude(c => c.Products)
            .ToListAsync();

        return _mapper.Map<IEnumerable<RestaurantDto>>(restaurants);
    }

    public async Task<RestaurantDto?> GetByIdAsync(Guid id)
    {
        var restaurant = await _context.Restaurants
            .Include(r => r.Categories)
            .ThenInclude(c => c.Products)
            .FirstOrDefaultAsync(r => r.Id == id);

        return restaurant == null ? null : _mapper.Map<RestaurantDto>(restaurant);
    }

    public async Task<RestaurantDto> CreateAsync(CreateRestaurantDto dto)
    {
        var restaurant = _mapper.Map<Restaurant>(dto);
        _context.Restaurants.Add(restaurant);
        await _context.SaveChangesAsync();
        return _mapper.Map<RestaurantDto>(restaurant);
    }

    public async Task<RestaurantDto?> UpdateAsync(Guid id, UpdateRestaurantDto dto)
    {
        var restaurant = await _context.Restaurants.FindAsync(id);
        if (restaurant == null) return null;

        _mapper.Map(dto, restaurant);
        await _context.SaveChangesAsync();
        return _mapper.Map<RestaurantDto>(restaurant);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var restaurant = await _context.Restaurants.FindAsync(id);
        if (restaurant == null) return false;

        _context.Restaurants.Remove(restaurant);
        await _context.SaveChangesAsync();
        return true;
    }
} 