using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QR_Menu.Application.Orders.DTOs;
using QR_Menu.Domain;
using QR_Menu.Infrastructure;

namespace QR_Menu.Application.Orders;

public class OrdersService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<OrdersService> _logger;

    public OrdersService(AppDbContext context, IMapper mapper, ILogger<OrdersService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<(OrderReadDto? Order, string? Error)> CreateAsync(Guid userId, OrderCreateDto dto)
    {
        var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.Id == dto.RestaurantId);
        if (restaurant == null) return (null, "Restoran bulunamadı.");

        // Load all products in one query
        var productIds = dto.Items.Select(i => i.ProductId).ToList();
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id) && p.RestaurantId == dto.RestaurantId && p.IsActive)
            .ToDictionaryAsync(p => p.Id);

        if (products.Count != productIds.Count) return (null, "Sepetteki ürünlerin bazıları bulunamadı veya pasif.");

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RestaurantId = dto.RestaurantId,
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            CustomerName = dto.CustomerName,
            CustomerTel = dto.CustomerTel,
            IsInPerson = dto.IsInPerson,
            Items = new List<OrderItem>()
        };

        foreach (var dtoItem in dto.Items)
        {
            var product = products[dtoItem.ProductId];
            var unitPrice = product.Price;
            var qty = dtoItem.Quantity;
            var lineTotal = unitPrice * qty;

            order.Items.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = product.Id,
                ProductNameSnapshot = product.Name,
                UnitPrice = unitPrice,
                Quantity = qty,
                LineTotal = lineTotal,
                OptionsJson = dtoItem.OptionsJson,
                CreatedAt = DateTime.UtcNow
            });
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Reload with navigation for mapping
        var saved = await _context.Orders
            .Include(o => o.Restaurant)
            .Include(o => o.Items!)
            .FirstAsync(o => o.Id == order.Id);

        return (_mapper.Map<OrderReadDto>(saved), null);
    }

    public async Task<(List<OrderReadDto> Orders, int Total)> GetByRestaurantAsync(
        Guid restaurantId,
        OrderStatus? status,
        DateTime? startDate,
        DateTime? endDate,
        int page = 1,
        int pageSize = 20)
    {
        var query = _context.Orders
            .AsNoTracking()
            .Include(o => o.Restaurant)
            .Include(o => o.Items)
            .Where(o => o.RestaurantId == restaurantId);

        if (status.HasValue) query = query.Where(o => o.Status == status);
        if (startDate.HasValue) query = query.Where(o => o.CreatedAt >= startDate.Value);
        if (endDate.HasValue) query = query.Where(o => o.CreatedAt <= endDate.Value);

        var total = await query.CountAsync();
        var list = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (_mapper.Map<List<OrderReadDto>>(list), total);
    }

    public async Task<OrderReadDto?> GetByIdAsync(Guid orderId)
    {
        var order = await _context.Orders
            .Include(o => o.Restaurant)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);
        return order == null ? null : _mapper.Map<OrderReadDto>(order);
    }

    public async Task<bool> UpdateStatusAsync(Guid orderId, OrderStatus status)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) return false;
        order.Status = status;
        await _context.SaveChangesAsync();
        return true;
    }
} 