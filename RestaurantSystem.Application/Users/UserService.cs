using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RestaurantSystem.Domain.Interfaces;
using RestaurantSystem.Application.Users.DTOs;
using RestaurantSystem.Domain;

namespace RestaurantSystem.Application.Users;

public class UserService
{
    private readonly IAppDbContext _context;
    private readonly IEmailService _emailService;
    private static readonly Dictionary<string, (string Code, DateTime Expiry)> _verificationCodes = new();

    public UserService(IAppDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task<(bool Success, string Message)> RegisterAsync(RegisterUserDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
        {
            return (false, "Email already registered");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = dto.Email,
            PasswordHash = HashPassword(dto.Password),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Tel = dto.Tel,
            City = dto.City,
            District = dto.District,
            Neighbourhood = dto.Neighbourhood,
            Role = UserRole.Customer,
            IsVerified = false
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Generate and send verification code
        var verificationCode = GenerateVerificationCode();
        _verificationCodes[dto.Email] = (verificationCode, DateTime.UtcNow.AddMinutes(15));
        await _emailService.SendVerificationEmailAsync(dto.Email, verificationCode);

        return (true, "Registration successful. Please check your email for verification code.");
    }

    public async Task<(bool Success, string Message)> VerifyEmailAsync(VerifyEmailDto dto)
    {
        if (!_verificationCodes.TryGetValue(dto.Email, out var verificationData))
        {
            return (false, "No verification code found for this email");
        }

        if (DateTime.UtcNow > verificationData.Expiry)
        {
            _verificationCodes.Remove(dto.Email);
            return (false, "Verification code has expired");
        }

        if (verificationData.Code != dto.VerificationCode)
        {
            return (false, "Invalid verification code");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null)
        {
            return (false, "User not found");
        }

        user.IsVerified = true;
        await _context.SaveChangesAsync();
        _verificationCodes.Remove(dto.Email);

        return (true, "Email verified successfully");
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    private string GenerateVerificationCode()
    {
        return new Random().Next(100000, 999999).ToString();
    }
} 