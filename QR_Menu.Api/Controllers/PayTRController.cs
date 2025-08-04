using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using QR_Menu.Application.Payments;
using QR_Menu.Application.Payments.DTOs;
using QR_Menu.Application.Common;
using QR_Menu.Infrastructure.Authorization;
using QR_Menu.Domain.Common;
using QR_Menu.Domain;
using QR_Menu.PayTRService.Services;
using QR_Menu.PayTRService.Models;
using System.Security.Claims;
using System.Text.Json;
using System.Text;
using System.Web;
using Microsoft.EntityFrameworkCore;
using QR_Menu.Infrastructure;
using Microsoft.Extensions.Options;

namespace QR_Menu.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PayTRController : BaseController
{
    private readonly PaymentService _paymentService;
    private readonly IPayTRAPIService _payTRAPIService;
    private readonly IPayTRSecurityService _payTRSecurityService;
    private readonly PayTRConfiguration _payTRConfig;
    private readonly AppDbContext _context;
    private readonly ILogger<PayTRController> _logger;

    public PayTRController(
        PaymentService paymentService,
        IPayTRAPIService payTRAPIService,
        IPayTRSecurityService payTRSecurityService,
        IOptions<PayTRConfiguration> payTRConfig,
        AppDbContext context,
        ILogger<PayTRController> logger)
    {
        _paymentService = paymentService;
        _payTRAPIService = payTRAPIService;
        _payTRSecurityService = payTRSecurityService;
        _payTRConfig = payTRConfig.Value;
        _context = context;
        _logger = logger;
    }

    [HttpPost("extend-license")]
    [RequirePermission(Permissions.Licenses.Extend)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponsBase>> ExtendLicenseByPay([FromBody] ExtendLicenseByPayDto request)
    {
        try
        {
            // Get current user from JWT
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            var isDealerStr = User.FindFirstValue("isDealer");
            
            if (userIdStr == null || !Guid.TryParse(userIdStr, out var currentUserId))
                return Unauthorized("Geçersiz kullanıcı", "Invalid user");

            // Determine target user (current user or admin-specified user)
            var targetUserId = request.AdminTargetUserId ?? currentUserId;
            var isAdminOperation = request.AdminTargetUserId.HasValue;

            // Validate target user
            var targetUser = await _context.Users
                .FirstOrDefaultAsync(u => (u.Email == request.UserEmail && u.PhoneNumber == request.UserPhoneNumber) ||
                                         (isAdminOperation && u.Id == targetUserId));
            
            if (targetUser == null)
                return NotFound("Kullanıcı bulunamadı", "User not found");

            // Parse user basket
            var userBasket = JsonSerializer.Deserialize<PayTRUserBasketForExtendLicenseDto>(request.UserBasket);
            if (userBasket == null)
                return BadRequest("Geçersiz sepet verisi", "Invalid basket data");

            // Validate restaurant
            if (!Guid.TryParse(userBasket.restaurantId, out var restaurantId))
                return BadRequest("Geçersiz restoran ID", "Invalid restaurant ID");

            var restaurant = await _context.Restaurants.FindAsync(restaurantId);
            if (restaurant == null)
                return NotFound("Restoran bulunamadı", "Restaurant not found");

            // Validate license
            if (!Guid.TryParse(userBasket.licenseId, out var licenseId))
                return BadRequest("Geçersiz lisans ID", "Invalid license ID");

            var license = await _context.Licenses.FindAsync(licenseId);
            if (license == null)
                return NotFound("Lisans bulunamadı", "License not found");

            // Validate license package
            if (!Guid.TryParse(userBasket.licensePackageId, out var licensePackageId))
                return BadRequest("Geçersiz lisans paketi ID", "Invalid license package ID");

            var licensePackage = await _context.LicensePackages.FindAsync(licensePackageId);
            if (licensePackage == null)
                return NotFound("Lisans paketi bulunamadı", "License package not found");

            // Calculate total amount with dealer discount
            var isDealer = bool.Parse(isDealerStr ?? "false") || (isAdminOperation && bool.Parse(isDealerStr ?? "false"));
            var totalAmount = isDealer ? licensePackage.DealerPrice : licensePackage.UserPrice;

            // Create payment basket
            var basket = new PaymentBasketDto
            {
                RestaurantId = restaurantId,
                RestaurantName = restaurant.Name,
                Username = targetUser.FullName,
                Licenses = new PaymentBasketLicenseDto
                {
                    LicenseId = licenseId,
                    LicensePackageId = licensePackageId,
                    LicensePackageName = licensePackage.Name,
                    LicensePackageTypeId = licensePackage.TimeId,
                    LicensePackageTime = licensePackage.Time,
                    LicensePackagePrice = totalAmount
                }
            };

            // Generate order number and process payment
            var orderNumber = _paymentService.GenerateOrderNumber();
            var userIp = _paymentService.GetClientIpAddress(HttpContext);
            var basketJson = JsonSerializer.Serialize(basket);

            // Create secure PayTR payment
            var payTRRequest = await _payTRSecurityService.CreateSecureDirectPaymentRequestAsync(
                userIp: userIp,
                merchantOid: orderNumber,
                email: request.UserEmail,
                amount: (decimal)totalAmount,
                cardNumber: request.CardNumber,
                expiryMonth: request.ExpiryMonth,
                expiryYear: request.ExpiryYear,
                cvv: request.CVV,
                cardOwner: request.CCOwner,
                userName: request.UserName,
                userAddress: request.UserAddress,
                userPhone: request.UserPhoneNumber,
                userBasket: basketJson
            );

            // Process payment through PayTR
            var (response, success, errorMessage) = await _payTRAPIService.PayAsync<PayTRDirectAPIPaymentDTO, string>(
                payTRRequest, HttpContext.RequestAborted);

            if (!success)
                return BadRequest($"Ödeme işlemi başarısız: {errorMessage}", $"Payment failed: {errorMessage}");

            // Create payment record
            await _paymentService.CreatePaymentRecordForLicenseAsync(
                targetUserId, targetUser.FullName, orderNumber, (decimal)totalAmount, 
                basketJson, PaymentLicenseType.ExtendLicense);

            return Success(response, "Lisans uzatma ödemesi alındı", "License extension payment received");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing license extension payment");
            return BadRequest("Ödeme işlemi sırasında hata oluştu", "Error occurred during payment processing");
        }
    }

    [HttpPost("add-license")]
    [RequirePermission(Permissions.Licenses.Create)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponsBase>> AddLicenseByPay([FromBody] AddLicenseByPayDto request)
    {
        try
        {
            // Get current user from JWT
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            var isDealerStr = User.FindFirstValue("isDealer");
            
            if (userIdStr == null || !Guid.TryParse(userIdStr, out var currentUserId))
                return Unauthorized("Geçersiz kullanıcı", "Invalid user");

            // Determine target user (current user or admin-specified user)
            var targetUserId = request.AdminTargetUserId ?? currentUserId;
            var isAdminOperation = request.AdminTargetUserId.HasValue;

            // Validate target user
            var targetUser = await _context.Users
                .FirstOrDefaultAsync(u => (u.Email == request.UserEmail && u.PhoneNumber == request.UserPhoneNumber) ||
                                         (isAdminOperation && u.Id == targetUserId));
            
            if (targetUser == null)
                return NotFound("Kullanıcı bulunamadı", "User not found");

            // Parse user baskets
            var userBaskets = JsonSerializer.Deserialize<List<PayTRUserBasketForNewLicenseDto>>(request.UserBasket);
            if (userBaskets == null || !userBaskets.Any())
                return BadRequest("Geçersiz sepet verisi", "Invalid basket data");

            var baskets = new List<PaymentBasketDto>();
            var totalAmount = 0.0;
            var isDealer = bool.Parse(isDealerStr ?? "false") || (isAdminOperation && bool.Parse(isDealerStr ?? "false"));

            // Process each restaurant basket
            foreach (var userBasket in userBaskets)
            {
                if (!Guid.TryParse(userBasket.restaurantId, out var restaurantId))
                    continue;

                var restaurant = await _context.Restaurants.FindAsync(restaurantId);
                if (restaurant == null) continue;

                var basketLicenses = new List<PaymentBasketLicenseDto>();

                foreach (var licensePackageIdStr in userBasket.licensePackageIds)
                {
                    if (!Guid.TryParse(licensePackageIdStr, out var licensePackageId))
                        continue;

                    var licensePackage = await _context.LicensePackages.FindAsync(licensePackageId);
                    if (licensePackage == null) continue;

                    var price = isDealer ? licensePackage.DealerPrice : licensePackage.UserPrice;
                    totalAmount += price;

                    basketLicenses.Add(new PaymentBasketLicenseDto
                    {
                        LicenseId = Guid.NewGuid(),
                        LicensePackageId = licensePackageId,
                        LicensePackageName = licensePackage.Name,
                        LicensePackageTypeId = licensePackage.TimeId,
                        LicensePackageTime = licensePackage.Time,
                        LicensePackagePrice = price
                    });
                }

                if (basketLicenses.Any())
                {
                    baskets.Add(new PaymentBasketDto
                    {
                        RestaurantId = restaurantId,
                        RestaurantName = restaurant.Name,
                        Username = targetUser.FullName,
                        Licenses = basketLicenses
                    });
                }
            }

            if (!baskets.Any())
                return BadRequest("Geçerli lisans paketi bulunamadı", "No valid license packages found");

            // Generate order number and process payment
            var orderNumber = _paymentService.GenerateOrderNumber();
            var userIp = _paymentService.GetClientIpAddress(HttpContext);
            var basketsJson = JsonSerializer.Serialize(baskets);

            // Create secure PayTR payment
            var payTRRequest = await _payTRSecurityService.CreateSecureDirectPaymentRequestAsync(
                userIp: userIp,
                merchantOid: orderNumber,
                email: request.UserEmail,
                amount: (decimal)totalAmount,
                cardNumber: request.CardNumber,
                expiryMonth: request.ExpiryMonth,
                expiryYear: request.ExpiryYear,
                cvv: request.CVV,
                cardOwner: request.CCOwner,
                userName: request.UserName,
                userAddress: request.UserAddress,
                userPhone: request.UserPhoneNumber,
                userBasket: basketsJson
            );

            // Process payment through PayTR
            var (response, success, errorMessage) = await _payTRAPIService.PayAsync<PayTRDirectAPIPaymentDTO, string>(
                payTRRequest, HttpContext.RequestAborted);

            if (!success)
                return BadRequest($"Ödeme işlemi başarısız: {errorMessage}", $"Payment failed: {errorMessage}");

            // Create payment record
            await _paymentService.CreatePaymentRecordForLicenseAsync(
                targetUserId, targetUser.FullName, orderNumber, (decimal)totalAmount, 
                basketsJson, PaymentLicenseType.NewLicense);

            return Success(response, "Yeni lisans ödemesi alındı", "New license payment received");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing new license payment");
            return BadRequest("Ödeme işlemi sırasında hata oluştu", "Error occurred during payment processing");
        }
    }

    [HttpPost("create-payment-link")]
    [RequirePermission(Permissions.Orders.Create)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResponsBase>> CreatePaymentLink([FromBody] CreatePaymentLinkDto request)
    {
        try
        {
            var orderNumber = _paymentService.GenerateOrderNumber();
            var price = ((decimal)(request.TotalPrice ?? 0) * 100).ToString("0");

            var payTRRequest = await _payTRSecurityService.CreateSecureCreateLinkRequestAsync(
                name: request.Products,
                price: price,
                maxInstallment: request.Installment.ToString(),
                maxCount: request.StockQuantity.ToString(),
                expiryDate: request.ExpiryDate,
                callbackId: orderNumber,
                getQr: request.CreateQR ? 1 : 0
            );

            var (response, success, errorMessage) = await _payTRAPIService.CreateLinkAsync<PayTRCreateLinkAPIPaymentDTO, PayTRCreateLinkAPIPaymentResponseDTO>(
                payTRRequest, HttpContext.RequestAborted);

            if (!success || response?.status != "success")
                return BadRequest($"Link oluşturma başarısız: {errorMessage}", $"Link creation failed: {errorMessage}");

            // Save QR code if requested
            string qrFilePath = "";
            if (request.CreateQR && !string.IsNullOrEmpty(response.base64_qr))
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "qr-codes");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{orderNumber}_{response.id}_{DateTime.UtcNow:yyyyMMddHHmmss}.png";
                var filePath = Path.Combine(uploadsFolder, fileName);

                await System.IO.File.WriteAllBytesAsync(filePath, Convert.FromBase64String(response.base64_qr));
                qrFilePath = $"/qr-codes/{fileName}";
            }

            // Create payment record
            await _paymentService.CreatePaymentRecordForLicenseAsync(
                Guid.Empty, "Link Payment", orderNumber, (decimal)(request.TotalPrice ?? 0), 
                request.Products, PaymentLicenseType.Link);

            // Include QR path in response
            var responseData = new { 
                response.id, 
                response.link, 
                response.status,
                qr_path = qrFilePath 
            };

            return Success(responseData, "Ödeme linki oluşturuldu", "Payment link created");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment link");
            return BadRequest("Link oluşturma sırasında hata oluştu", "Error occurred while creating link");
        }
    }

    [HttpPost("delete-payment-link")]
    [RequirePermission(Permissions.Orders.Delete)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResponsBase>> DeletePaymentLink([FromBody] DeletePaymentLinkDto request)
    {
        try
        {
            var payTRRequest = await _payTRSecurityService.CreateSecureDeleteLinkRequestAsync(request.Id);

            var (response, success, errorMessage) = await _payTRAPIService.DeleteLinkAsync<PayTRDeleteLinkAPIPaymentDTO, string>(
                payTRRequest, HttpContext.RequestAborted);

            if (!success)
                return BadRequest($"Link silme başarısız: {errorMessage}", $"Link deletion failed: {errorMessage}");

            return Success(response, "Ödeme linki silindi", "Payment link deleted");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting payment link");
            return BadRequest("Link silme sırasında hata oluştu", "Error occurred while deleting link");
        }
    }

    [AllowAnonymous]
    [HttpPost("callback")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Callback()
    {
        try
        {
            // Read request body
            var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var requestBody = await reader.ReadToEndAsync();

            if (string.IsNullOrEmpty(requestBody))
                return Ok("OK");

            // Parse form data
            var parsedData = HttpUtility.ParseQueryString(requestBody);

            var callbackDto = new PayTRCallbackDTO
            {
                CallbackId = parsedData["callback_id"],
                Hash = parsedData["hash"],
                MerchantOid = parsedData["merchant_oid"],
                Status = parsedData["status"],
                PaymentType = parsedData["payment_type"],
                FailedReasonMsg = parsedData["failed_reason_msg"],
                TestMode = parsedData["test_mode"] == "1"
            };

            if (int.TryParse(parsedData["merchant_id"], out int merchantId))
                callbackDto.MerchantId = merchantId;
            else
                return Ok("OK");

            if (decimal.TryParse(parsedData["total_amount"], out decimal totalAmount))
                callbackDto.TotalAmount = totalAmount;

            if (int.TryParse(parsedData["failed_reason_code"], out int failedReasonCode))
                callbackDto.FailedReasonCode = failedReasonCode;

            // Skip failed payments
            if (callbackDto.Status == "failed")
            {
                _logger.LogWarning("Failed payment callback received: {OrderNumber}", callbackDto.MerchantOid);
                return Ok("OK");
            }

            // Validate callback hash
            if (!_payTRSecurityService.ValidateCallbackHash(callbackDto))
            {
                _logger.LogWarning("Invalid callback hash for order: {OrderNumber}", callbackDto.MerchantOid);
                return Ok("OK");
            }

            // Find and process payment
            var payment = await _paymentService.GetPaymentByOrderNumberAsync(callbackDto.MerchantOid ?? "");
            if (payment == null)
            {
                _logger.LogWarning("Payment not found for callback: {OrderNumber}", callbackDto.MerchantOid);
                return Ok("OK");
            }

            // Process the payment callback
            var result = await _paymentService.ProcessLicensePaymentCallbackAsync(payment, callbackDto.Status ?? "failed");
            
            if (result)
            {
                _logger.LogInformation("Payment callback processed successfully: {OrderNumber}", callbackDto.MerchantOid);
            }
            else
            {
                _logger.LogError("Failed to process payment callback: {OrderNumber}", callbackDto.MerchantOid);
            }

            return Ok("OK");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PayTR callback");
            return Ok("OK"); // Always return OK to PayTR
        }
    }

    [AllowAnonymous]
    [HttpPost("payment-failure")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ContentResult PaymentFailure()
    {
        string htmlResponse = @"
<!DOCTYPE html>
<html lang='tr'>
  <head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Ödeme Başarısız</title>
    <style>
      body { font-family: Arial, sans-serif; text-align: center; padding: 50px; }
      .error { color: #d32f2f; }
    </style>
  </head>
  <body>
    <div class='error'>
      <h1>Ödeme Başarısız</h1>
      <p>Ödemeniz işlenemedi. Lütfen tekrar deneyiniz.</p>
      <p>Payment Failed. Please try again.</p>
    </div>
    <script>
      // Notify parent window
      if (window.parent) {
        window.parent.postMessage({ status: 'failed' }, '*');
      }
      // Auto close after 5 seconds
      setTimeout(() => {
        if (window.close) window.close();
      }, 5000);
    </script>
  </body>
</html>";

        return new ContentResult
        {
            Content = htmlResponse,
            ContentType = "text/html",
            StatusCode = 200
        };
    }
} 