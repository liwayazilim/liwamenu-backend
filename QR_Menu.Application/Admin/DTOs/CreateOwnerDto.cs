using System.ComponentModel.DataAnnotations;

namespace QR_Menu.Application.Admin.DTOs;

public class CreateOwnerDto
{
    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    public string? City { get; set; }
    public string? District { get; set; }
    public string? Neighbourhood { get; set; }

    public bool IsActive { get; set; } = true;
    public bool EmailConfirmed { get; set; } = false;
    
    public Guid? DealerId { get; set; } // Optional: Assign to a dealer immediately
} 