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
    private readonly AppDbContext _context;

    public UserService(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IEmailService emailService,
        IMapper mapper,
        AppDbContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _emailService = emailService;
        _mapper = mapper;
        _context = context;
    }

    public async Task<(bool Success, string Message)> RegisterAsync(RegisterUserDto dto)
    {
        var user = new User
        {
            UserName = dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            PhoneNumber = dto.PhoneNumber,
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
    public async Task<(List<UserReadDto> Users, int TotalCount)> GetAllAsync(string? searchKey = null, int pageNumber = 1, int pageSize = 20)
    {
        var query = _userManager.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(searchKey))
        {
            query = query.Where(u => u.FirstName.Contains(searchKey) || u.LastName.Contains(searchKey) || u.Email.Contains(searchKey) || u.PhoneNumber.Contains(searchKey));
        }
        var total = await query.CountAsync();
        var users = await query.OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (_mapper.Map<List<UserReadDto>>(users), total);
    }

    public async Task<UserReadDto?> GetByIdAsync(Guid id)
    {
        var user = await _userManager.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        return user == null ? null : _mapper.Map<UserReadDto>(user);
    }

    public async Task<(UserReadDto? User, string? ErrorMessage)> CreateAsync(UserCreateDto dto)
    {
        
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password) || 
            string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName))
        {
            return (null, "Geçersiz istek. Tüm gerekli alanlar doldurulmalıdır.");
        }

        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser != null)
        {
            return (null, "Kullanıcı zaten var.");
        }

        // Check if phone number already exists
        var existingUserByPhone = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == dto.PhoneNumber);
        if (existingUserByPhone != null)
        {
            return (null, "Telefon numarası zaten kullanılıyor.");
        }

        // Verify dealer exists if DealerId is provided
        if (dto.DealerId.HasValue)
        {
            var dealer = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == dto.DealerId.Value && u.IsDealer);
            if (dealer == null)
            {
                return (null, "Bayi bulunamadı.");
            }
        }

        // Create user
        var user = new User
        {
            UserName = dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            PhoneNumber = dto.PhoneNumber,
            CreatedDateTime = DateTime.UtcNow,
            LastUpdateDateTime = DateTime.UtcNow,
            City = dto.City,
            District = dto.District,
            Neighbourhood = dto.Neighbourhood,
            DealerId = dto.DealerId,
            Note = dto.Note,
            SendSMSNotify = dto.SendSMSNotify,
            SendEmailNotify = dto.SendEmailNotify,
            Role = Enum.TryParse<UserRole>(dto.Role, out var role) ? role : UserRole.Customer,
            IsActive = dto.IsActive,
            IsDealer = dto.IsDealer,
            EmailConfirmed = true // Admin/Dealer created users are verified by default
            
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            return (null, "Kullanıcı oluşturulamadı.");
        }

        await _userManager.AddToRoleAsync(user, user.Role.ToString());

        // Send notifications if requested
        if (dto.SendEmailNotify)
        {
            try
            {
                var emailMessage = $"Hoş geldiniz {dto.FirstName} {dto.LastName}!\n\n" +
                                 $"Hesabınız başarıyla oluşturuldu.\n" +
                                 $"E-posta: {dto.Email}\n" +
                                 $"Şifre: {dto.Password}\n\n" +
                                 $"Güvenlik için lütfen şifrenizi değiştirin.";
                
                await _emailService.SendPasswordResetEmailAsync(dto.Email, "Yeni Kullanıcı Kaydı - Pentegrasyon");
            }
            catch (Exception ex)
            {
                // Log email sending error but don't fail the user creation
                // You might want to add proper logging here
            }
        }

       /* if (dto.SendSMSNotify)
        {
            try
            {
                // SMS notification logic would go here
                // You would need to implement SMS service similar to _telsamAPI
                // For now, we'll just log that SMS should be sent
                // await _smsService.SendSMS(dto.PhoneNumber, $"Hoş geldiniz {dto.FirstName}! Hesabınız oluşturuldu. E-posta: {dto.Email}, Şifre: {dto.Password}");
            }
            catch (Exception ex)
            {
                // Log SMS sending error but don't fail the user creation
                // You might want to add proper logging here
            }
        }
         */

        return (_mapper.Map<UserReadDto>(user), null);
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
        if (dto.PhoneNumber != null && dto.PhoneNumber != "string") user.PhoneNumber = dto.PhoneNumber;
        if (dto.Role != null && dto.Role != "string" && Enum.TryParse<UserRole>(dto.Role, out var role)) user.Role = role;
        if (dto.IsActive.HasValue) user.IsActive = dto.IsActive.Value;
        if (dto.IsDealer.HasValue) user.IsDealer = dto.IsDealer.Value;
        if (dto.City != null && dto.City != "string") user.City = dto.City;
        if (dto.District != null && dto.District != "string") user.District = dto.District;
        if (dto.Neighbourhood != null && dto.Neighbourhood != "string") user.Neighbourhood = dto.Neighbourhood;
        if (dto.DealerId.HasValue) user.DealerId = dto.DealerId;
        if (dto.Note != null && dto.Note != "string") user.Note = dto.Note;
        if (dto.PassiveNote != null && dto.PassiveNote != "string") user.PassiveNote = dto.PassiveNote;
        if (dto.SendSMSNotify.HasValue) user.SendSMSNotify = dto.SendSMSNotify.Value;
        if (dto.SendEmailNotify.HasValue) user.SendEmailNotify = dto.SendEmailNotify.Value;
        
        // Always update LastUpdateDateTime for any changes
        user.LastUpdateDateTime = DateTime.UtcNow;
        
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateUserIsActiveAsync(Guid userId, bool isActive, string? passiveNote = null)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) 
            return (false, "Kullanıcı bulunamadı.");
        
        // Ensure SecurityStamp is set (required by Identity)
        if (string.IsNullOrEmpty(user.SecurityStamp))
        {
            user.SecurityStamp = Guid.NewGuid().ToString();
        }
        
        user.LastUpdateDateTime = DateTime.UtcNow;
        user.IsActive = isActive;
        
        // Update passive note only if provided or if user is being deactivated
        if (!string.IsNullOrWhiteSpace(passiveNote))
        {
            user.PassiveNote = passiveNote;
        }
        else if (!isActive && string.IsNullOrWhiteSpace(user.PassiveNote))
        {
            // Set default passive note if user is being deactivated and no note provided
            user.PassiveNote = "Kullanıcı deaktif edildi";
        }
        
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded ? (true, null) : (false, "Kullanıcı güncellenirken hata oluştu.");
    }

    public async Task<(bool success, string? errorMessage)> UpdateUserIsVerifiedAsync(Guid userId, bool isVerify)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if(user == null) return (false, "Kullanıcı bulunamadı");

        if(string.IsNullOrEmpty(user.SecurityStamp))
        {
            user.SecurityStamp = Guid.NewGuid().ToString();
        }

        user.EmailConfirmed = isVerify;
        user.LastUpdateDateTime = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);

        return result.Succeeded ? (true, null) : (false, "Kullanıcı güncellenirken hata oluştu.");
    }

    public async Task<(bool success, string? errorMessage)> UpdateUserIsDealerAsync(Guid userId, bool isDealer, bool sendSMSNotify = false, bool sendEmailNotify = false)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) 
            return (false, "Kullanıcı bulunamadı.");
        
        // Ensure SecurityStamp is set (required by Identity)
        if (string.IsNullOrEmpty(user.SecurityStamp))
        {
            user.SecurityStamp = Guid.NewGuid().ToString();
        }
        
        user.LastUpdateDateTime = DateTime.UtcNow;
        user.IsDealer = isDealer;
        
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return (false, "Kullanıcı güncellenirken hata oluştu.");
        }

        // Send notifications if requested
        if (sendEmailNotify)
        {
            try
            {
                var emailMessage = $"Merhaba {user.FirstName} {user.LastName}!\n\n" +
                                 $"Bayi durumunuz güncellendi.\n" +
                                 $"Yeni durum: {(isDealer ? "Bayi" : "Normal Kullanıcı")}\n\n" +
                                 $"Bu değişiklik hakkında sorularınız varsa lütfen bizimle iletişime geçin.";
                
                await _emailService.SendPasswordResetEmailAsync(user.Email, "Bayi Durumu Güncellendi - QR Menu");
            }
            catch (Exception ex)
            {
                // Log email sending error but don't fail the user update
                // You might want to add proper logging here
            }
        }

        if (sendSMSNotify)
        {
            try
            {
                // SMS notification logic would go here
                // You would need to implement SMS service similar to your original _telsamAPI
                // For now, we'll just log that SMS should be sent
                // await _smsService.SendSMS(user.PhoneNumber, $"Bayi durumunuz güncellendi: {(isDealer ? "Bayi" : "Normal Kullanıcı")}");
            }
            catch (Exception ex)
            {
                // Log SMS sending error but don't fail the user update
                // You might want to add proper logging here
            }
        }

        return (true, null);
    }

    public async Task<(bool success, string? errorMessage)> DealerTransferAsync(Guid userId, Guid dealerUserId)
    {
        // Get the user to be transferred
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return (false, "Kullanıcı bulunamadı.");

        // Get the dealer user
        var dealer = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == dealerUserId && u.IsDealer);
        if (dealer == null)
            return (false, "Bayi bulunamadı.");

        // Ensure SecurityStamp is set (required by Identity)
        if (string.IsNullOrEmpty(user.SecurityStamp))
        {
            user.SecurityStamp = Guid.NewGuid().ToString();
        }

        // Update user's dealer assignment
        user.DealerId = dealer.Id;
        user.LastUpdateDateTime = DateTime.UtcNow;

        var userUpdateResult = await _userManager.UpdateAsync(user);
        if (!userUpdateResult.Succeeded)
        {
            return (false, "Kullanıcı güncellenirken hata oluştu.");
        }

        // Update all restaurants owned by this user to have the new dealer
        var restaurants = await _context.Restaurants
            .Where(r => r.UserId == userId)
            .ToListAsync();

        if (restaurants.Any())
        {
            foreach (var restaurant in restaurants)
            {
                restaurant.DealerId = dealer.Id;
                
            }

            await _context.SaveChangesAsync();
        }

        return (true, null);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return false;
        var result = await _userManager.DeleteAsync(user);
        return result.Succeeded;
    }

   

  
} 