using Microsoft.EntityFrameworkCore;
using RestaurantSystem.Domain;

namespace RestaurantSystem.Domain.Interfaces;

public interface IAppDbContext
{
    DbSet<User> Users { get; set; }
    DbSet<Restaurant> Restaurants { get; set; }
    DbSet<Category> Categories { get; set; }
    DbSet<Product> Products { get; set; }
    DbSet<Order> Orders { get; set; }
    DbSet<License> Licenses { get; set; }
    DbSet<PaymentMethod> PaymentMethods { get; set; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
} 