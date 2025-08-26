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
    public DbSet<LicensePackage> LicensePackages { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<OrderTag> OrderTags { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<PaymentMethod> PaymentMethods { get; set; }
    public DbSet<Payment> Payments { get; set; }

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

        // License-LicensePackage
        modelBuilder.Entity<License>()
            .HasOne(l => l.LicensePackage)
            .WithMany(lp => lp.Licenses)
            .HasForeignKey(l => l.LicensePackageId)
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

        // Order-OrderItem one-to-many with payload
        modelBuilder.Entity<Order>()
            .HasMany(o => o.Items)
            .WithOne(oi => oi.Order!)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Product>()
            .HasMany(p => p.OrderItems)
            .WithOne(oi => oi.Product!)
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OrderItem>()
            .Property(oi => oi.LineTotal)
            .HasPrecision(18, 2);
        modelBuilder.Entity<OrderItem>()
            .Property(oi => oi.UnitPrice)
            .HasPrecision(18, 2);
        modelBuilder.Entity<OrderItem>()
            .Property(oi => oi.DiscountedUnitPrice)
            .HasPrecision(18, 2);
        modelBuilder.Entity<OrderItem>()
            .Property(oi => oi.TaxAmount)
            .HasPrecision(18, 2);
        modelBuilder.Entity<OrderItem>()
            .Property(oi => oi.FinalLineTotal)
            .HasPrecision(18, 2);

        // Order-OrderTag many-to-many (order-level tags)
        modelBuilder.Entity<Order>()
            .HasMany(o => o.Tags)
            .WithMany(ot => ot.Orders)
            .UsingEntity(j => j.ToTable("OrderOrderTags"));

        // OrderItem-OrderTag many-to-many (item-level tags)
        modelBuilder.Entity<OrderItem>()
            .HasMany(oi => oi.Tags)
            .WithMany(ot => ot.OrderItems)
            .UsingEntity(j => j.ToTable("OrderItemOrderTags"));

        // Restaurant-OrderTag
        modelBuilder.Entity<Restaurant>()
            .HasMany(r => r.OrderTags)
            .WithOne(ot => ot.Restaurant)
            .HasForeignKey(ot => ot.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);

        // Product decimal precision
        modelBuilder.Entity<Product>()
            .Property(p => p.Price)
            .HasPrecision(18, 2);

        // Order decimal precision
        modelBuilder.Entity<Order>()
            .Property(o => o.SubTotal)
            .HasPrecision(18, 2);
        modelBuilder.Entity<Order>()
            .Property(o => o.TaxAmount)
            .HasPrecision(18, 2);
        modelBuilder.Entity<Order>()
            .Property(o => o.DiscountAmount)
            .HasPrecision(18, 2);
        modelBuilder.Entity<Order>()
            .Property(o => o.TotalAmount)
            .HasPrecision(18, 2);

        // Restaurant-PaymentMethod many-to-many
        modelBuilder.Entity<Restaurant>()
            .HasMany(r => r.PaymentMethods)
            .WithMany(pm => pm.Restaurants)
            .UsingEntity(j => j.ToTable("RestaurantPaymentMethods"));

        // Payment relationships
        modelBuilder.Entity<Payment>()
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Restaurant)
            .WithMany()
            .HasForeignKey(p => p.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Order)
            .WithMany()
            .HasForeignKey(p => p.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        // Ensure OrderNumber is unique
        modelBuilder.Entity<Payment>()
            .HasIndex(p => p.OrderNumber)
            .IsUnique();

        // Performance indexes for better query performance
        modelBuilder.Entity<Restaurant>()
            .HasIndex(r => new { r.UserId, r.IsActive })
            .HasDatabaseName("IX_Restaurant_UserId_IsActive");

        modelBuilder.Entity<Restaurant>()
            .HasIndex(r => new { r.City, r.District, r.IsActive })
            .HasDatabaseName("IX_Restaurant_City_District_IsActive");

        modelBuilder.Entity<Category>()
            .HasIndex(c => new { c.RestaurantId, c.IsActive, c.DisplayOrder })
            .HasDatabaseName("IX_Category_RestaurantId_IsActive_DisplayOrder");

        modelBuilder.Entity<Product>()
            .HasIndex(p => new { p.RestaurantId, p.CategoryId, p.IsActive, p.IsAvailable })
            .HasDatabaseName("IX_Product_RestaurantId_CategoryId_IsActive_IsAvailable");

        modelBuilder.Entity<Product>()
            .HasIndex(p => new { p.RestaurantId, p.IsFeatured, p.IsActive })
            .HasDatabaseName("IX_Product_RestaurantId_IsFeatured_IsActive");

        modelBuilder.Entity<Order>()
            .HasIndex(o => new { o.RestaurantId, o.Status, o.CreatedAt })
            .HasDatabaseName("IX_Order_RestaurantId_Status_CreatedAt");

        modelBuilder.Entity<Order>()
            .HasIndex(o => new { o.UserId, o.CreatedAt })
            .HasDatabaseName("IX_Order_UserId_CreatedAt");

        modelBuilder.Entity<OrderItem>()
            .HasIndex(oi => new { oi.OrderId, oi.ProductId })
            .HasDatabaseName("IX_OrderItem_OrderId_ProductId");

        modelBuilder.Entity<OrderTag>()
            .HasIndex(ot => new { ot.RestaurantId, ot.TagType, ot.IsActive, ot.DisplayOrder })
            .HasDatabaseName("IX_OrderTag_RestaurantId_TagType_IsActive_DisplayOrder");
    }
} 