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
using QR_Menu.PayTRService.Helpers;
using System.Security.Claims;
using System.Text.Json;
using System.Text;
using System.Web;
using Microsoft.EntityFrameworkCore;
using QR_Menu.Infrastructure;

namespace QR_Menu.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PayTRController : BaseController
{
    private readonly PaymentService _paymentService;
    private readonly QR_Menu.PayTRService.Services.IPayTRAPIService _payTRAPIService;
    private readonly AppDbContext _context;
    private readonly ILogger<PayTRController> _logger;
    private readonly IConfiguration _configuration;

    public PayTRController(
        PaymentService paymentService,
        QR_Menu.PayTRService.Services.IPayTRAPIService payTRAPIService,
        AppDbContext context,
        ILogger<PayTRController> logger,
        IConfiguration configuration)
    {
        _paymentService = paymentService;
        _payTRAPIService = payTRAPIService;
        _context = context;
        _logger = logger;
        _configuration = configuration;
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
            var userBasket = JsonSerializer.Deserialize<PayTRUserBasketForExtendLicenseDTO>(request.UserBasket);
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
            var tokenData = new PayTRDirectAPITokenDTO
            {
                merchant_id = int.Parse(_configuration["PayTR:MerchantId"]),
                merchant_key = _configuration["PayTR:MerchantKey"],
                merchant_salt = _configuration["PayTR:MerchantSalt"],
                user_ip = userIp,
                merchant_oid = orderNumber,
                email = request.UserEmail,
                payment_amount = (double)totalAmount,
                payment_type = "card",
                installment_count = 0,
                currency = "TL",
                test_mode = bool.Parse(_configuration["PayTR:TestMode"]) ? "1" : "0",
                non_3d = "0"
            };

            var token = TokenHasherHelper.CreatePayTRToken(tokenData);

            var payTRRequest = new PayTRDirectAPIPaymentDTO
            {
                merchant_id = int.Parse(_configuration["PayTR:MerchantId"]),
                paytr_token = token,
                user_ip = userIp,
                merchant_oid = orderNumber,
                email = request.UserEmail,
                payment_amount = (double)totalAmount,
                payment_type = "card",
                installment_count = 0,
                currency = "TL",
                test_mode = bool.Parse(_configuration["PayTR:TestMode"]) ? "1" : "0",
                non_3d = "0",
                cc_owner = request.CCOwner,
                card_number = request.CardNumber,
                expiry_month = request.ExpiryMonth,
                expiry_year = request.ExpiryYear,
                cvv = request.CVV,
                user_name = request.UserName,
                user_address = request.UserAddress,
                user_phone = request.UserPhoneNumber,
                user_basket = basketJson,
                merchant_ok_url = _configuration["PayTR:SuccessUrl"],
                merchant_fail_url = _configuration["PayTR:FailUrl"]
            };

            // Process payment through PayTR
            var (response, statusCode) = await _payTRAPIService.Pay<PayTRDirectAPIPaymentDTO, string>(payTRRequest);

            if (statusCode != System.Net.HttpStatusCode.OK)
                return BadRequest($"Ödeme işlemi başarısız: HTTP {statusCode}", $"Payment failed: HTTP {statusCode}");

            // Create payment record
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrderNumber = orderNumber,
                UserId = targetUserId,
                Amount = (decimal)totalAmount,
                Currency = "TRY",
                PaymentMethod = PaymentType.PayTR,
                Status = PaymentStatus.Waiting,
                LicenseType = PaymentLicenseType.ExtendLicense,
                PayTRToken = token,
                BasketItems = basketJson,
                CustomerEmail = request.UserEmail,
                CustomerPhone = request.UserPhoneNumber,
                CustomerName = request.UserName,
                CreatedDateTime = DateTime.UtcNow,
                LastUpdateDateTime = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return Success(response, "Ödeme başarıyla alındı", "Payment received successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ExtendLicenseByPay");
            return BadRequest("Ödeme işlemi sırasında hata oluştu", "An error occurred during payment processing");
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
            var userBaskets = JsonSerializer.Deserialize<List<PayTRUserBasketForNewLicenseDTO>>(request.UserBasket);
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
                if (restaurant == null)
                    continue;

                var basketLicenses = new List<PaymentBasketLicenseDto>();

                foreach (var licensePackageIdStr in userBasket.licensePackageIds)
                {
                    if (!Guid.TryParse(licensePackageIdStr, out var licensePackageId))
                        continue;

                    var licensePackage = await _context.LicensePackages.FindAsync(licensePackageId);
                    if (licensePackage == null)
                        continue;

                    var licenseAmount = isDealer ? licensePackage.DealerPrice : licensePackage.UserPrice;
                    totalAmount += licenseAmount;

                    basketLicenses.Add(new PaymentBasketLicenseDto
                    {
                        LicenseId = Guid.NewGuid(),
                        LicensePackageId = licensePackageId,
                        LicensePackageName = licensePackage.Name,
                        LicensePackageTypeId = licensePackage.TimeId,
                        LicensePackageTime = licensePackage.Time,
                        LicensePackagePrice = licenseAmount
                    });
                }

                    baskets.Add(new PaymentBasketDto
                    {
                        RestaurantId = restaurantId,
                        RestaurantName = restaurant.Name,
                        Username = targetUser.FullName,
                        Licenses = basketLicenses
                    });
            }

            if (!baskets.Any())
                return BadRequest("Geçerli lisans paketi bulunamadı", "No valid license packages found");

            // Generate order number and process payment
            var orderNumber = _paymentService.GenerateOrderNumber();
            var userIp = _paymentService.GetClientIpAddress(HttpContext);
            var basketsJson = JsonSerializer.Serialize(baskets);

            // Create secure PayTR payment
            var tokenData = new PayTRDirectAPITokenDTO
            {
                merchant_id = int.Parse(_configuration["PayTR:MerchantId"]),
                merchant_key = _configuration["PayTR:MerchantKey"],
                merchant_salt = _configuration["PayTR:MerchantSalt"],
                user_ip = userIp,
                merchant_oid = orderNumber,
                email = request.UserEmail,
                payment_amount = totalAmount,
                payment_type = "card",
                installment_count = 0,
                currency = "TL",
                test_mode = bool.Parse(_configuration["PayTR:TestMode"]) ? "1" : "0",
                non_3d = "0"
            };

            var token = TokenHasherHelper.CreatePayTRToken(tokenData);

            var payTRRequest = new PayTRDirectAPIPaymentDTO
            {
                merchant_id = int.Parse(_configuration["PayTR:MerchantId"]),
                paytr_token = token,
                user_ip = userIp,
                merchant_oid = orderNumber,
                email = request.UserEmail,
                payment_amount = totalAmount,
                payment_type = "card",
                installment_count = 0,
                currency = "TL",
                test_mode = bool.Parse(_configuration["PayTR:TestMode"]) ? "1" : "0",
                non_3d = "0",
                cc_owner = request.CCOwner,
                card_number = request.CardNumber,
                expiry_month = request.ExpiryMonth,
                expiry_year = request.ExpiryYear,
                cvv = request.CVV,
                user_name = request.UserName,
                user_address = request.UserAddress,
                user_phone = request.UserPhoneNumber,
                user_basket = basketsJson,
                merchant_ok_url = _configuration["PayTR:SuccessUrl"],
                merchant_fail_url = _configuration["PayTR:FailUrl"]
            };

            // Process payment through PayTR
            var (response, statusCode) = await _payTRAPIService.Pay<PayTRDirectAPIPaymentDTO, string>(payTRRequest);

            if (statusCode != System.Net.HttpStatusCode.OK)
                return BadRequest($"Ödeme işlemi başarısız: HTTP {statusCode}", $"Payment failed: HTTP {statusCode}");

            // Create payment record
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrderNumber = orderNumber,
                UserId = targetUserId,
                Amount = (decimal)totalAmount,
                Currency = "TRY",
                PaymentMethod = PaymentType.PayTR,
                Status = PaymentStatus.Waiting,
                LicenseType = PaymentLicenseType.NewLicense,
                PayTRToken = token,
                BasketItems = basketsJson,
                CustomerEmail = request.UserEmail,
                CustomerPhone = request.UserPhoneNumber,
                CustomerName = request.UserName,
                CreatedDateTime = DateTime.UtcNow,
                LastUpdateDateTime = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return Success(response, "Ödeme başarıyla alındı", "Payment received successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AddLicenseByPay");
            return BadRequest("Ödeme işlemi sırasında hata oluştu", "An error occurred during payment processing");
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

            var tokenData = new PayTRCreateLinkAPITokenDTO
            {
                name = request.Products,
                price = price,
                currency = "TL",
                max_installment = request.Installment.ToString(),
                link_type = "product",
                lang = "tr",
                min_count = "0",
                merchant_key = _configuration["PayTR:MerchantKey"],
                merchant_salt = _configuration["PayTR:MerchantSalt"]
            };

            var token = TokenHasherHelper.CreatePayTRToken(tokenData);

            var payTRRequest = new PayTRCreateLinkAPIPaymentDTO
            {
                merchant_id = int.Parse(_configuration["PayTR:MerchantId"]),
                paytr_token = token,
                name = request.Products,
                price = price,
                currency = "TL",
                max_installment = request.Installment.ToString(),
                lang = "tr",
                get_qr = request.CreateQR ? "1" : "0",
                link_type = "product",
                min_count = "0",
                max_count = request.StockQuantity.ToString(),
                expiry_date = request.ExpiryDate.ToString("yyyy-MM-dd HH:mm:ss"),
                callback_link = _configuration["PayTR:CallbackUrl"],
                callback_id = orderNumber,
                debug_on = bool.Parse(_configuration["PayTR:DebugMode"]) ? 1 : 0
            };

            var (response, statusCode) = await _payTRAPIService.CreateLink<PayTRCreateLinkAPIPaymentDTO, PayTRCreateLinkAPIPaymentResponseDTO>(payTRRequest);

            if (statusCode != System.Net.HttpStatusCode.OK)
                return BadRequest($"Link oluşturma başarısız: HTTP {statusCode}", $"Link creation failed: HTTP {statusCode}");

            if (response?.status != "success")
            {
                var payTRError = response?.reason ?? "PayTR link creation failed";
                return BadRequest($"PayTR link hatası: {payTRError}", $"PayTR link error: {payTRError}");
            }

            // Create payment record
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrderNumber = orderNumber,
                Amount = (decimal)(request.TotalPrice ?? 0),
                Currency = "TRY",
                PaymentMethod = PaymentType.PayTR,
                Status = PaymentStatus.Waiting,
                LicenseType = PaymentLicenseType.Link,
                PayTRPaymentLink = response.link,
                PayTRPaymentLinkId = response.id,
                BasketItems = request.Products,
                CreatedDateTime = DateTime.UtcNow,
                LastUpdateDateTime = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return Success(response, "Ödeme linki başarıyla oluşturuldu", "Payment link created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CreatePaymentLink");
            return BadRequest("Link oluşturma sırasında hata oluştu", "An error occurred during link creation");
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
            var tokenData = new PayTRDeleteLinkAPITokenDTO
            {
                id = request.Id,
                merchant_id = int.Parse(_configuration["PayTR:MerchantId"]),
                merchant_key = _configuration["PayTR:MerchantKey"],
                merchant_salt = _configuration["PayTR:MerchantSalt"]
            };

            var token = TokenHasherHelper.CreatePayTRToken(tokenData);

            var payTRRequest = new PayTRDeleteLinkAPIPaymentDTO
            {
                id = request.Id,
                merchant_id = int.Parse(_configuration["PayTR:MerchantId"]),
                paytr_token = token,
                debug_on = bool.Parse(_configuration["PayTR:DebugMode"]) ? 1 : 0
            };

            var (response, statusCode) = await _payTRAPIService.DeleteLink<PayTRDeleteLinkAPIPaymentDTO, string>(payTRRequest);

            if (statusCode != System.Net.HttpStatusCode.OK)
                return BadRequest($"Link silme başarısız: HTTP {statusCode}", $"Link deletion failed: HTTP {statusCode}");

            return Success(response, "Ödeme linki başarıyla silindi", "Payment link deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DeletePaymentLink");
            return BadRequest("Link silme sırasında hata oluştu", "An error occurred during link deletion");
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

            var payTRCallback = new PayTRCallbackDTO
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
                payTRCallback.MerchantId = merchantId;

            if (decimal.TryParse(parsedData["total_amount"], out decimal totalAmount))
                payTRCallback.TotalAmount = totalAmount;

            if (int.TryParse(parsedData["failed_reason_code"], out int failedReasonCode))
                payTRCallback.FailedReasonCode = failedReasonCode;

            // If payment failed, just return OK
            if (payTRCallback.Status == "failed")
                return Ok("OK");

            // Find and update payment
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.OrderNumber == payTRCallback.MerchantOid && p.Status == PaymentStatus.Waiting);

            if (payment == null)
                return Ok("OK");

            payment.Status = payTRCallback.Status == "success" ? PaymentStatus.Success : PaymentStatus.Failed;
            payment.LastUpdateDateTime = DateTime.UtcNow;
            payment.PaymentDateTime = DateTime.UtcNow;

            if (payTRCallback.Status == "success")
            {
                await ProcessSuccessfulPayment(payment);
            }

            await _context.SaveChangesAsync();

            return Ok("OK");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in PayTR Callback");
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
<html lang='en'>
  <head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Payment Failed</title>
  </head>
  <body>
    <script>
        window.parent.postMessage({ status: 'failed' }, '*');
    </script>
    <p>Payment failed. Please close this window and try again.</p>
  </body>
</html>";

        return new ContentResult
        {
            Content = htmlResponse,
            ContentType = "text/html",
            StatusCode = 200
        };
    }

    private async Task ProcessSuccessfulPayment(Payment payment)
    {
        try
        {
            if (payment.LicenseType == PaymentLicenseType.NewLicense)
            {
                await ProcessNewLicensePayment(payment);
            }
            else if (payment.LicenseType == PaymentLicenseType.ExtendLicense)
            {
                await ProcessExtendLicensePayment(payment);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing successful payment for order: {OrderNumber}", payment.OrderNumber);
        }
    }

    private async Task ProcessNewLicensePayment(Payment payment)
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

    private async Task ProcessExtendLicensePayment(Payment payment)
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
                    var license = new License
            {
                Id = Guid.NewGuid(),
                RestaurantId = basket.RestaurantId,
                UserId = payment.UserId,
                LicensePackageId = licenseDto.LicensePackageId,
                CreatedDateTime = DateTime.UtcNow,
                LastUpdateDateTime = DateTime.UtcNow,
                StartDateTime = DateTime.UtcNow,
                EndDateTime = DateTime.UtcNow.AddYears(licenseDto.LicensePackageTime),
                IsActive = true
            };

        _context.Licenses.Add(license);
        await _context.SaveChangesAsync();

        _logger.LogInformation("License created for user {UserId}, restaurant {RestaurantId}, license {LicenseId}", 
            payment.UserId, basket.RestaurantId, license.Id);
    }

    private async Task ExtendLicenseFromPaymentAsync(Payment payment, PaymentBasketLicenseDto licenseDto)
    {
        var license = await _context.Licenses.FindAsync(licenseDto.LicenseId);
        if (license != null)
        {
            license.EndDateTime = license.EndDateTime.AddYears(licenseDto.LicensePackageTime);
            license.LastUpdateDateTime = DateTime.UtcNow;
            license.LicensePackageId = licenseDto.LicensePackageId;

            await _context.SaveChangesAsync();

            _logger.LogInformation("License extended for user {UserId}, license {LicenseId}", 
                payment.UserId, license.Id);
        }
    }
} 