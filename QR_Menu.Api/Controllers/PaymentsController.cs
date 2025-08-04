using Microsoft.AspNetCore.Mvc;
using QR_Menu.Application.Payments;
using QR_Menu.Application.Payments.DTOs;
using QR_Menu.Application.Common;
using QR_Menu.Domain;
using QR_Menu.Domain.Common;
using QR_Menu.Infrastructure.Authorization;

namespace QR_Menu.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : BaseController
{
    private readonly PaymentService _paymentService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(PaymentService paymentService, ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    /* 
    // TODO: These PayTR-specific endpoints have been commented out since PayTR logic
    // has been moved to a separate service. Implement integration with your separate PayTRService.
    
    [HttpPost("direct-payment")]
    [RequirePermission(Permissions.Orders.Create)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResponsBase>> ProcessDirectPayment([FromBody] PaymentCreateDto dto)
    {
        try
        {
            var userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            
            var (success, message, response) = await _paymentService.ProcessDirectPaymentAsync(dto, userIp);
            
            if (success)
            {
                return Success(response, "Ödeme başarıyla işlendi", "Payment processed successfully");
            }
            else
            {
                return BadRequest("Ödeme işlemi başarısız", message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing direct payment");
            return BadRequest("Ödeme işlemi sırasında hata oluştu", "Error occurred during payment processing");
        }
    }

    [HttpPost("create-link")]
    [RequirePermission(Permissions.Orders.Create)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResponsBase>> CreatePaymentLink([FromBody] PaymentCreateDto dto)
    {
        try
        {
            var userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var (success, message, response) = await _paymentService.CreatePaymentLinkAsync(dto, userIp);

            if (success)
            {
                return Success(response, "Ödeme linki başarıyla oluşturuldu", "Payment link created successfully");
            }
            else
            {
                return BadRequest("Ödeme linki oluşturulamadı", message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment link");
            return BadRequest("Ödeme linki oluşturulurken hata oluştu", "Error occurred while creating payment link");
        }
    }

    [HttpDelete("delete-link/{linkId}")]
    [RequirePermission(Permissions.Orders.Update)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResponsBase>> DeletePaymentLink(string linkId)
    {
        try
        {
            var (success, message) = await _paymentService.DeletePaymentLinkAsync(linkId);

            if (success)
            {
                return Success(null, "Ödeme linki başarıyla silindi", "Payment link deleted successfully");
            }
            else
            {
                return BadRequest("Ödeme linki silinemedi", message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting payment link: {LinkId}", linkId);
            return BadRequest("Ödeme linki silinirken hata oluştu", "Error occurred while deleting payment link");
        }
    }

    [HttpPost("callback")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResponsBase>> ProcessCallback([FromForm] PayTRCallbackRequest callback)
    {
        try
        {
            var (success, message) = await _paymentService.ProcessCallbackAsync(callback);

            if (success)
            {
                return Success(null, "Callback başarıyla işlendi", "Callback processed successfully");
            }
            else
            {
                return BadRequest("Callback işlemi başarısız", message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing callback");
            return BadRequest("Callback işlemi sırasında hata oluştu", "Error occurred during callback processing");
        }
    }
    */

    [HttpGet("GetPayments")]
    [RequirePermission(Permissions.Orders.ViewAll)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<object>> GetPayments(
        [FromQuery] Guid? userId = null,
        [FromQuery] Guid? restaurantId = null,
        [FromQuery] PaymentStatus? status = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int? pageNumber = null,
        [FromQuery] int? pageSize = null)
    {
        try
        {
            return await GetPaginatedDataAsync(
                async (page, size) => await _paymentService.GetPaymentsAsync(
                    userId, restaurantId, status, startDate, endDate, page, size),
                pageNumber,
                pageSize,
                "Ödemeler başarıyla getirildi",
                "Payments retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payments");
            return BadRequest("Ödemeler getirilirken hata oluştu", "Error occurred while retrieving payments");
        }
    }

    [HttpGet("GetMyPayments")]
    [RequirePermission(Permissions.Orders.ViewOwn)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<object>> GetMyPayments(
        [FromQuery] PaymentStatus? status = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int? pageNumber = null,
        [FromQuery] int? pageSize = null)
    {
        try
        {
            // Get current user ID from claims
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized("Kullanıcı kimliği bulunamadı", "User identity not found");
            }

            return await GetPaginatedDataAsync(
                async (page, size) => await _paymentService.GetPaymentsAsync(
                    userId, null, status, startDate, endDate, page, size),
                pageNumber,
                pageSize,
                "Ödemeleriniz başarıyla getirildi",
                "Your payments retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user payments");
            return BadRequest("Ödemeleriniz getirilirken hata oluştu", "Error occurred while retrieving your payments");
        }
    }

    [HttpGet("GetPaymentById")]
    [RequirePermission(Permissions.Orders.View)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponsBase>> GetPaymentById(Guid paymentId)
    {
        try
        {
            var payment = await _paymentService.GetPaymentByIdAsync(paymentId);
            
            if (payment == null)
            {
                return NotFound("Ödeme bulunamadı", "Payment not found");
            }

            return Success(payment, "Ödeme başarıyla getirildi", "Payment retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment: {PaymentId}", paymentId);
            return BadRequest("Ödeme getirilirken hata oluştu", "Error occurred while retrieving payment");
        }
    }

    [HttpGet("GetPaymentByOrderNumber")]
    [RequirePermission(Permissions.Orders.View)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponsBase>> GetPaymentByOrderNumber(string orderNumber)
    {
        try
        {
            var payment = await _paymentService.GetPaymentByOrderNumberAsync(orderNumber);
            
            if (payment == null)
            {
                return NotFound("Ödeme bulunamadı", "Payment not found");
            }

            return Success(payment, "Ödeme başarıyla getirildi", "Payment retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment by order number: {OrderNumber}", orderNumber);
            return BadRequest("Ödeme getirilirken hata oluştu", "Error occurred while retrieving payment");
        }
    }
} 