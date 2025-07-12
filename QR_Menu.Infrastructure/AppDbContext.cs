using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using QR_Menu.Domain;

namespace QR_Menu.Infrastructure;

public class AppDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Remove DbSet<User> (handled by Identity)
    public DbSet<Restaurant> Restaurants { get; set; }
    public DbSet<License> Licenses { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<PaymentMethod> PaymentMethods { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User-Dealer self reference
        modelBuilder.Entity<User>()
            .HasMany(u => u.Restaurants)
            .WithOne(r => r.User)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .HasMany(u => u.Licenses)
            .WithOne(l => l.User)
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Restaurant-License
        modelBuilder.Entity<Restaurant>()
            .HasOne(r => r.License)
            .WithOne(l => l.Restaurant)
            .HasForeignKey<Restaurant>(r => r.LicenseId)
            .OnDelete(DeleteBehavior.SetNull);

        // Restaurant-Category
        modelBuilder.Entity<Category>()
            .HasMany(c => c.Products)
            .WithOne(p => p.Category)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Restaurant>()
            .HasMany(r => r.Categories)
            .WithOne(c => c.Restaurant)
            .HasForeignKey(c => c.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Restaurant-Product
        modelBuilder.Entity<Restaurant>()
            .HasMany(r => r.Products)
            .WithOne(p => p.Restaurant)
            .HasForeignKey(p => p.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Order-Product many-to-many
        modelBuilder.Entity<Order>()
            .HasMany(o => o.Products)
            .WithMany(p => p.Orders)
            .UsingEntity(j => j.ToTable("OrderProducts"));

        // Restaurant-PaymentMethod many-to-many
        modelBuilder.Entity<Restaurant>()
            .HasMany<PaymentMethod>("PaymentMethods")
            .WithMany("Restaurants")
            .UsingEntity(j => j.ToTable("RestaurantPaymentMethods"));
    }
} 