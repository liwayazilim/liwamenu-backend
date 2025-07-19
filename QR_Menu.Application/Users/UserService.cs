using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using QR_Menu.Application.Users.DTOs;
using QR_Menu.Domain;
using QR_Menu.Infrastructure;
using QR_Menu.Domain.Common.Interfaces;
using AutoMapper;

namespace QR_Menu.Application.Users;

public class UserService
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly IEmailService _emailService;
    private readonly IMapper _mapper;

    public UserService(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IEmailService emailService,
        IMapper mapper)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _emailService = emailService;
        _mapper = mapper;
    }

    public async Task<(bool Success, string Message)> RegisterAsync(RegisterUserDto dto)
    {
        var user = new User
        {
            UserName = dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            PhoneNumber = dto.Tel,
            City = dto.City,
            District = dto.District,
            Role = UserRole.Customer,
            IsActive = true
        };
        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            var msg = string.Join(", ", result.Errors.Select(e => e.Description));
            return (false, msg);
        }
        try
        {
            await _userManager.AddToRoleAsync(user, UserRole.Customer.ToString());
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            await _emailService.SendVerificationEmailAsync(user.Email, token);
        }
        catch (Exception ex)
        {
            // Clean up user if any step fails
            await _userManager.DeleteAsync(user);
            return (false, $"Registration failed: {ex.Message}");
        }
        return (true, "Registration successful. Please check your email for verification link.");
    }

    public async Task<(bool Success, string Message)> VerifyEmailAsync(VerifyEmailDto dto)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null)
        {
            return (false, "User not found");
        }
        var result = await _userManager.ConfirmEmailAsync(user, dto.VerificationCode);
        if (!result.Succeeded)
        {
            var msg = string.Join(", ", result.Errors.Select(e => e.Description));
            return (false, msg);
        }
        return (true, "Email verified successfully");
    }

    public async Task<(bool Success, string Message)> ForgotPasswordAsync(string emailOrPhone)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == emailOrPhone || u.PhoneNumber == emailOrPhone);
        if (user == null)
            return (false, "User not found");
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        await _emailService.SendPasswordResetEmailAsync(user.Email, token);
        return (true, "Password reset link or code sent to your email.");
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(string emailOrPhone, string code, string newPassword)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == emailOrPhone || u.PhoneNumber == emailOrPhone);
        if (user == null)
            return (false, "User not found");
        var result = await _userManager.ResetPasswordAsync(user, code, newPassword);
        if (!result.Succeeded)
        {
            var msg = string.Join(", ", result.Errors.Select(e => e.Description));
            return (false, msg);
        }
        return (true, "Password has been reset successfully.");
    }

    public async Task<(bool Success, string Message)> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        // Use the same query method as GetByIdAsync for consistency
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return (false, "User not found");
        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
        {
            var msg = string.Join(", ", result.Errors.Select(e => e.Description));
            return (false, msg);
        }
        return (true, "Password changed successfully.");
    }

    public async Task<(bool Success, string Message, User? User)> LoginAsync(string emailOrPhone, string password)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == emailOrPhone || u.PhoneNumber == emailOrPhone);
        if (user == null)
            return (false, "User not found", null);
        if (!user.IsActive)
            return (false, "User is not active", null);
        if (!user.EmailConfirmed)
            return (false, "User email is not verified", null);
        var result = await _signInManager.CheckPasswordSignInAsync(user, password, false);
        if (!result.Succeeded)
            return (false, "Invalid credentials", null);
        return (true, "Login successful", user);
    }

    // User CRUD (use UserManager for user management)
    public async Task<(List<UserReadDto> Users, int TotalCount)> GetAllAsync(string? search = null, int page = 1, int pageSize = 20)
    {
        var query = _userManager.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u => u.FirstName.Contains(search) || u.LastName.Contains(search) || u.Email.Contains(search) || u.PhoneNumber.Contains(search));
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
        var user = await _userManager.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        return user == null ? null : _mapper.Map<UserReadDto>(user);
    }

    public async Task<UserReadDto?> CreateAsync(UserCreateDto dto)
    {
        var user = new User
        {
            UserName = dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            PhoneNumber = dto.Tel,
            City = dto.City,
            District = dto.District,
            Neighbourhood = dto.Neighbourhood,
            Role = Enum.TryParse<UserRole>(dto.Role, out var role) ? role : UserRole.Customer,
            IsActive = dto.IsActive,
            IsDealer = dto.IsDealer,
            EmailConfirmed = true // Admin/Dealer created users are verified by default
        };
        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            return null;
        await _userManager.AddToRoleAsync(user, user.Role.ToString());
        return _mapper.Map<UserReadDto>(user);
    }

    public async Task<bool> UpdateAsync(Guid id, UserUpdateDto dto)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return false;
        
        // Ensure SecurityStamp is set (required by Identity)
        if (string.IsNullOrEmpty(user.SecurityStamp))
        {
            user.SecurityStamp = Guid.NewGuid().ToString();
        }
        
        if (dto.FirstName != null && dto.FirstName != "string") user.FirstName = dto.FirstName;
        if (dto.LastName != null && dto.LastName != "string") user.LastName = dto.LastName;
        if (dto.Tel != null && dto.Tel != "string") user.PhoneNumber = dto.Tel;
        if (dto.Role != null && dto.Role != "string" && Enum.TryParse<UserRole>(dto.Role, out var role)) user.Role = role;
        // Only update boolean values if they are explicitly provided (not default)
        if (dto.IsActive.HasValue && dto.IsActive != user.IsActive) user.IsActive = dto.IsActive.Value;
        if (dto.IsDealer.HasValue && dto.IsDealer != user.IsDealer) user.IsDealer = dto.IsDealer.Value;
        if (dto.City != null && dto.City != "string") user.City = dto.City;
        if (dto.District != null && dto.District != "string") user.District = dto.District;
        if (dto.Neighbourhood != null && dto.Neighbourhood != "string") user.Neighbourhood = dto.Neighbourhood;
        
        // Handle email update carefully - ensure UserName is always set
        if (dto.Email != null && !string.IsNullOrWhiteSpace(dto.Email) && dto.Email != "string")
        {
            user.Email = dto.Email;
            user.UserName = dto.Email; 
        }
        else
        {
            
            if (string.IsNullOrWhiteSpace(user.UserName))
            {
                user.UserName = user.Email ?? $"user_{user.Id}";
            }
        }
        
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return false;
        var result = await _userManager.DeleteAsync(user);
        return result.Succeeded;
    }

    // Helper: Get user by email
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _userManager.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    // Helper: Generate email confirmation token for a user by Id
    public async Task<string> GenerateEmailConfirmationTokenAsync(Guid userId)
    {
        
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) throw new Exception("User not found");
        return await _userManager.GenerateEmailConfirmationTokenAsync(user);
    }

    // Helper: Send verification email
    public async Task SendVerificationEmailAsync(string email, string token)
    {
        await _emailService.SendVerificationEmailAsync(email, token);
    }
} 