using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using RestaurantSystem.Application.Users.DTOs;
using RestaurantSystem.Domain;
using RestaurantSystem.Infrastructure;
using RestaurantSystem.Domain.Common.Interfaces;
using AutoMapper;

namespace RestaurantSystem.Application.Users;

public class UserService
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private static readonly Dictionary<string, (string Code, DateTime Expiry)> _verificationCodes = new();
    private static readonly Dictionary<string, (string Code, DateTime Expiry)> _resetCodes = new();
    private readonly PasswordHasher<User> _passwordHasher = new();
    private readonly IMapper _mapper;

    public UserService(AppDbContext context, IEmailService emailService, IMapper mapper)
    {
        _context = context;
        _emailService = emailService;
        _mapper = mapper;
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
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Tel = dto.Tel,
            City = dto.City,
            District = dto.District,
            Neighbourhood = dto.Neighbourhood,
            Role = UserRole.Customer,
            IsVerified = false
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);
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

    private bool VerifyPassword(User user, string password)
    {
        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result == PasswordVerificationResult.Success;
    }

    private string GenerateVerificationCode()
    {
        return new Random().Next(100000, 999999).ToString();
    }

    public async Task<(bool Success, string Message, User? User)> LoginAsync(string emailOrPhone, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == emailOrPhone || u.Tel == emailOrPhone);
        if (user == null)
            return (false, "User not found", null);
        if (!user.IsActive)
            return (false, "User is not active", null);
        if (!user.IsVerified)
            return (false, "User is not verified", null);
        if (!VerifyPassword(user, password))
            return (false, "Invalid credentials", null);
        return (true, "Login successful", user);
    }

    public async Task<(bool Success, string Message)> ForgotPasswordAsync(string emailOrPhone)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == emailOrPhone || u.Tel == emailOrPhone);
        if (user == null)
            return (false, "User not found");
        var code = GenerateVerificationCode();
        _resetCodes[user.Email] = (code, DateTime.UtcNow.AddMinutes(15));
        await _emailService.SendVerificationEmailAsync(user.Email, code);
        return (true, "Password reset code sent to your email.");
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(string emailOrPhone, string code, string newPassword)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == emailOrPhone || u.Tel == emailOrPhone);
        if (user == null)
            return (false, "User not found");
        if (!_resetCodes.TryGetValue(user.Email, out var resetData))
            return (false, "No reset code found for this user");
        if (DateTime.UtcNow > resetData.Expiry)
        {
            _resetCodes.Remove(user.Email);
            return (false, "Reset code has expired");
        }
        if (resetData.Code != code)
            return (false, "Invalid reset code");
        user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
        await _context.SaveChangesAsync();
        _resetCodes.Remove(user.Email);
        return (true, "Password has been reset successfully.");
    }

    public async Task<(bool Success, string Message)> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return (false, "User not found");
        if (!VerifyPassword(user, currentPassword))
            return (false, "Current password is incorrect");
        user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
        await _context.SaveChangesAsync();
        return (true, "Password changed successfully.");
    }

    // User CRUD
    public async Task<(List<UserReadDto> Users, int TotalCount)> GetAllAsync(string? search = null, int page = 1, int pageSize = 20)
    {
        var query = _context.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u => u.FirstName.Contains(search) || u.LastName.Contains(search) || u.Email.Contains(search) || u.Tel.Contains(search));
        }
        var total = await query.CountAsync();
        var users = await query.OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (_mapper.Map<List<UserReadDto>>(users), total);
    }

    public async Task<UserReadDto?> GetByIdAsync(Guid id)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        return user == null ? null : _mapper.Map<UserReadDto>(user);
    }

    public async Task<UserReadDto?> CreateAsync(UserCreateDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            return null;
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            Tel = dto.Tel,
            City = dto.City,
            District = dto.District,
            Neighbourhood = dto.Neighbourhood,
            Role = Enum.TryParse<UserRole>(dto.Role, out var role) ? role : UserRole.Customer,
            IsActive = dto.IsActive,
            IsDealer = dto.IsDealer,
            IsVerified = true // Admin/Dealer created users are verified by default
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return _mapper.Map<UserReadDto>(user);
    }

    public async Task<bool> UpdateAsync(Guid id, UserUpdateDto dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return false;
        if (dto.FirstName != null) user.FirstName = dto.FirstName;
        if (dto.LastName != null) user.LastName = dto.LastName;
        if (dto.Email != null) user.Email = dto.Email;
        if (dto.Tel != null) user.Tel = dto.Tel;
        if (dto.Role != null && Enum.TryParse<UserRole>(dto.Role, out var role)) user.Role = role;
        if (dto.IsActive.HasValue) user.IsActive = dto.IsActive.Value;
        if (dto.IsDealer.HasValue) user.IsDealer = dto.IsDealer.Value;
        if (dto.City != null) user.City = dto.City;
        if (dto.District != null) user.District = dto.District;
        if (dto.Neighbourhood != null) user.Neighbourhood = dto.Neighbourhood;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return false;
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return true;
    }
} 