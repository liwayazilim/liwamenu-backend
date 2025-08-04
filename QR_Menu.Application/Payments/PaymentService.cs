using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QR_Menu.Application.Payments.DTOs;
using QR_Menu.Domain;
using QR_Menu.Infrastructure;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace QR_Menu.Application.Payments;

public class PaymentService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        AppDbContext context,
        IMapper mapper,
        ILogger<PaymentService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<(PaymentReadDto? Payment, string? ErrorMessage)> CreatePaymentAsync(PaymentCreateDto dto, string userIp)
    {
        try
        {
            _logger.LogInformation("Creating payment for user: {UserId}, Amount: {Amount}", dto.UserId, dto.Amount);

            // Generate unique order number
            var orderNumber = GenerateOrderNumber();

            // Create payment record
            var payment = _mapper.Map<Payment>(dto);
            payment.OrderNumber = orderNumber;
            payment.CreatedDateTime = DateTime.UtcNow;
            payment.LastUpdateDateTime = DateTime.UtcNow;

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Payment record created with order number: {OrderNumber}", orderNumber);

            var paymentDto = _mapper.Map<PaymentReadDto>(payment);
            return (paymentDto, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment for user: {UserId}", dto.UserId);
            return (null, $"Payment creation error: {ex.Message}");
        }
    }

    public async Task<(List<PaymentReadDto> Payments, int TotalCount)> GetPaymentsAsync(
        Guid? userId = null,
        Guid? restaurantId = null,
        PaymentStatus? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int pageNumber = 1,
        int pageSize = 20)
    {
        try
        {
            var query = _context.Payments
                .Include(p => p.User)
                .Include(p => p.Restaurant)
                .AsNoTracking();

            // Apply filters
            if (userId.HasValue)
                query = query.Where(p => p.UserId == userId.Value);

            if (restaurantId.HasValue)
                query = query.Where(p => p.RestaurantId == restaurantId.Value);

            if (status.HasValue)
                query = query.Where(p => p.Status == status.Value);

            if (startDate.HasValue)
                query = query.Where(p => p.CreatedDateTime >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(p => p.CreatedDateTime <= endDate.Value);

            var totalCount = await query.CountAsync();

            var payments = await query
                .OrderByDescending(p => p.CreatedDateTime)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paymentDtos = _mapper.Map<List<PaymentReadDto>>(payments);

            return (paymentDtos, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payments");
            return (new List<PaymentReadDto>(), 0);
        }
    }

    public async Task<PaymentReadDto?> GetPaymentByIdAsync(Guid paymentId)
    {
        try
        {
            var payment = await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Restaurant)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            return payment != null ? _mapper.Map<PaymentReadDto>(payment) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment by ID: {PaymentId}", paymentId);
            return null;
        }
    }

    public async Task<Payment?> GetPaymentByOrderNumberAsync(string orderNumber)
    {
        try
        {
            return await _context.Payments
                .FirstOrDefaultAsync(p => p.OrderNumber == orderNumber && p.Status == PaymentStatus.Waiting);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment by order number: {OrderNumber}", orderNumber);
            return null;
        }
    }

    public async Task<bool> UpdatePaymentStatusAsync(Guid paymentId, PaymentStatus status, string? errorMessage = null)
    {
        try
        {
            var payment = await _context.Payments.FindAsync(paymentId);
            if (payment == null)
                return false;

            payment.Status = status;
            payment.LastUpdateDateTime = DateTime.UtcNow;
            
            if (status == PaymentStatus.Success)
                payment.PaymentDateTime = DateTime.UtcNow;
            
            if (!string.IsNullOrEmpty(errorMessage))
                payment.ErrorMessage = errorMessage;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment status: {PaymentId}", paymentId);
            return false;
        }
    }

    public async Task<(PaymentReadDto? Payment, string? ErrorMessage)> CreatePaymentRecordForLicenseAsync(
        Guid userId, 
        string userName, 
        string orderNumber, 
        decimal amount, 
        string basketJson, 
        PaymentLicenseType licenseType)
    {
        try
        {
            _logger.LogInformation("Creating license payment record for user: {UserId}, Amount: {Amount}", userId, amount);

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrderNumber = orderNumber,
                UserId = userId,
                Amount = amount,
                Currency = "TRY",
                PaymentMethod = PaymentType.PayTR,
                Status = PaymentStatus.Waiting,
                LicenseType = licenseType,
                BasketItems = basketJson,
                CustomerName = userName,
                CreatedDateTime = DateTime.UtcNow,
                LastUpdateDateTime = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("License payment record created with order number: {OrderNumber}", orderNumber);

            var paymentDto = _mapper.Map<PaymentReadDto>(payment);
            return (paymentDto, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating license payment record for user: {UserId}", userId);
            return (null, $"Payment creation error: {ex.Message}");
        }
    }

    public async Task<bool> ProcessLicensePaymentCallbackAsync(Payment payment, string status)
    {
        try
        {
            // Update payment status
            payment.Status = status.ToLower() switch
            {
                "success" => PaymentStatus.Success,
                "failed" => PaymentStatus.Failed,
                _ => PaymentStatus.Cancelled
            };
            payment.LastUpdateDateTime = DateTime.UtcNow;
            if (payment.Status == PaymentStatus.Success)
                payment.PaymentDateTime = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Process license operations if payment successful
            if (payment.Status == PaymentStatus.Success)
            {
                await ProcessSuccessfulLicensePaymentAsync(payment);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing license payment callback for payment: {PaymentId}", payment.Id);
            return false;
        }
    }

    private async Task ProcessSuccessfulLicensePaymentAsync(Payment payment)
    {
        try
        {
            switch (payment.LicenseType)
            {
                case PaymentLicenseType.NewLicense:
                    await ProcessNewLicensePaymentAsync(payment);
                    break;
                case PaymentLicenseType.ExtendLicense:
                    await ProcessExtendLicensePaymentAsync(payment);
                    break;
                case PaymentLicenseType.Link:
                    // Link payments don't require additional processing
                    _logger.LogInformation("Payment link payment processed: {OrderNumber}", payment.OrderNumber);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing successful license payment: {PaymentId}", payment.Id);
            throw;
        }
    }

    private async Task ProcessNewLicensePaymentAsync(Payment payment)
    {
        var baskets = JsonSerializer.Deserialize<List<PaymentBasketDto>>(payment.BasketItems ?? "[]");
        if (baskets == null) return;

        foreach (var basket in baskets)
        {
            if (basket.Licenses is JsonElement licensesElement)
            {
                var licenses = JsonSerializer.Deserialize<List<PaymentBasketLicenseDto>>(licensesElement.GetRawText());
                if (licenses != null)
                {
                    foreach (var licenseDto in licenses)
                    {
                        await CreateLicenseFromPaymentAsync(payment, basket, licenseDto);
                    }
                }
            }
        }
    }

    private async Task ProcessExtendLicensePaymentAsync(Payment payment)
    {
        var basket = JsonSerializer.Deserialize<PaymentBasketDto>(payment.BasketItems ?? "{}");
        if (basket?.Licenses is JsonElement licenseElement)
        {
            var licenseDto = JsonSerializer.Deserialize<PaymentBasketLicenseDto>(licenseElement.GetRawText());
            if (licenseDto != null)
            {
                await ExtendLicenseFromPaymentAsync(payment, licenseDto);
            }
        }
    }

    private async Task CreateLicenseFromPaymentAsync(Payment payment, PaymentBasketDto basket, PaymentBasketLicenseDto licenseDto)
    {
        var licensePackage = await _context.LicensePackages.FindAsync(licenseDto.LicensePackageId);
        if (licensePackage == null) return;

        var license = new License
        {
            Id = Guid.NewGuid(),
            UserId = payment.UserId,
            RestaurantId = basket.RestaurantId,
            LicensePackageId = licenseDto.LicensePackageId,
            StartDateTime = DateTime.UtcNow,
            EndDateTime = licensePackage.TimeId == 0 
                ? DateTime.UtcNow.AddMonths(licensePackage.Time)
                : DateTime.UtcNow.AddYears(licensePackage.Time),
            IsActive = true,
            UserPrice = licensePackage.UserPrice,
            DealerPrice = licensePackage.DealerPrice,
            CreatedDateTime = DateTime.UtcNow,
            LastUpdateDateTime = DateTime.UtcNow
        };

        _context.Licenses.Add(license);
        await _context.SaveChangesAsync();

        _logger.LogInformation("License created from payment: UserId={UserId}, LicenseId={LicenseId}, RestaurantId={RestaurantId}", 
            payment.UserId, license.Id, basket.RestaurantId);
    }

    private async Task ExtendLicenseFromPaymentAsync(Payment payment, PaymentBasketLicenseDto licenseDto)
    {
        var license = await _context.Licenses.FindAsync(licenseDto.LicenseId);
        if (license == null) return;

        var licensePackage = await _context.LicensePackages.FindAsync(licenseDto.LicensePackageId);
        if (licensePackage == null) return;

        // Extend the license
        if (licensePackage.TimeId == 0) // Months
        {
            license.EndDateTime = license.EndDateTime.AddMonths(licensePackage.Time);
        }
        else // Years
        {
            license.EndDateTime = license.EndDateTime.AddYears(licensePackage.Time);
        }

        license.LicensePackageId = licenseDto.LicensePackageId;
        license.LastUpdateDateTime = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("License extended from payment: UserId={UserId}, LicenseId={LicenseId}", 
            payment.UserId, license.Id);
    }

    public string GenerateOrderNumber()
    {
        // Generate a unique order number: QR-YYYYMMDD-HHMMSS-RANDOM
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var random = new Random().Next(1000, 9999);
        return $"QR-{timestamp}-{random}";
    }

    public string GetClientIpAddress(HttpContext httpContext)
    {
        // Try to get the real IP address from various headers
        var ipAddress = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (string.IsNullOrEmpty(ipAddress))
        {
            ipAddress = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        }
        if (string.IsNullOrEmpty(ipAddress))
        {
            ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        }
        
        return ipAddress ?? "127.0.0.1";
    }
} 