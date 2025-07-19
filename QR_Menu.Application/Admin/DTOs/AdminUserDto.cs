namespace QR_Menu.Application.Admin.DTOs;

public class AdminUserDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsDealer { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? Neighbourhood { get; set; }
    public Guid? DealerId { get; set; }
    public string? DealerName { get; set; }

    // Statistics
    public int RestaurantsCount { get; set; }
    public int LicensesCount { get; set; }
    public int ActiveRestaurantsCount { get; set; }
    public int ActiveLicensesCount { get; set; }
   
    // Timespans
    public DateTime? LastLoginDate { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public int AccessFailedCount { get; set; }
} 