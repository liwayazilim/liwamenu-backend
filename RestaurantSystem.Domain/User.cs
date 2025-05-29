namespace RestaurantSystem.Domain;

public enum UserRole
{
    Admin,
    Dealer,
    Owner,
    Customer
}

public class User
{
    public Guid Id { get; set; }
    public Guid? DealerId { get; set; } // For users linked to a dealer
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string Tel { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsVerified { get; set; } = false;
    public bool IsDealer { get; set; } = false;
    public string? City { get; set; }
    public string? District { get; set; }
    public string? Neighbourhood { get; set; }
    public ICollection<Restaurant>? Restaurants { get; set; }
    public ICollection<License>? Licenses { get; set; }
} 