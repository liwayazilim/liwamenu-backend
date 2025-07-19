using Microsoft.AspNetCore.Identity;

namespace QR_Menu.Domain;

public enum UserRole
{
    Admin,
    Dealer,
    Owner,
    Customer
}

public class User : IdentityUser<Guid>
{
    public Guid? DealerId { get; set; } // For users linked to a dealer
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDealer { get; set; } = false;
    public string? City { get; set; }
    public string? District { get; set; }
    public string? Neighbourhood { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public ICollection<Restaurant>? Restaurants { get; set; }
    public ICollection<License>? Licenses { get; set; }
    
    // Navigation properties for dealer relationships
    public User? Dealer { get; set; } // The dealer assigned to this user
    public ICollection<User>? ManagedUsers { get; set; } // Users managed by this dealer
} 