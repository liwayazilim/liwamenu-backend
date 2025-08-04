using QR_Menu.PayTRService.Models;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Reflection;

namespace QR_Menu.PayTRService.Services
{
    public class PayTRAPIClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PayTRAPIClient> _logger;

        public PayTRAPIClient(HttpClient httpClient, ILogger<PayTRAPIClient> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Configure default headers
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "QR-Menu-PayTR-Client/1.0");
        }

        public async Task<(TResponse?, HttpStatusCode, string? ErrorMessage)> SendRequestAsync<TRequest, TResponse>(
            HttpMethod method,
            string url,
            TRequest requestData,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Log request without sensitive data
                LogSecureRequest(method, url, requestData);

                using var request = new HttpRequestMessage(method, url);
                
                // Build form content based on request type
                var formData = BuildFormData(requestData);
                if (formData.Any())
                {
                    request.Content = new FormUrlEncodedContent(formData);
                }

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("PayTR API Error: Status {StatusCode}, Reason: {ReasonPhrase}",
                        response.StatusCode, response.ReasonPhrase);
                    
                    return (default(TResponse), response.StatusCode, $"API Error: {response.StatusCode} - {response.ReasonPhrase}");
                }

                // Log success without sensitive response data
                _logger.LogDebug("PayTR API request successful for {Method} {Url}", method.Method, url);

                // Handle string responses
                if (typeof(TResponse) == typeof(string))
                {
                    return ((TResponse)(object)responseContent, response.StatusCode, null);
                }

                // Deserialize JSON response
                try
                {
                    var deserializedResponse = JsonSerializer.Deserialize<TResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    
                    return (deserializedResponse, response.StatusCode, null);
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "Failed to deserialize PayTR response");
                    return (default(TResponse), response.StatusCode, "Failed to deserialize response");
                }
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP request failed for PayTR API: {Method} {Url}", method.Method, url);
                return (default(TResponse), HttpStatusCode.RequestTimeout, $"HTTP Error: {httpEx.Message}");
            }
            catch (TaskCanceledException tcEx) when (tcEx.InnerException is TimeoutException)
            {
                _logger.LogError(tcEx, "PayTR API request timeout: {Method} {Url}", method.Method, url);
                return (default(TResponse), HttpStatusCode.RequestTimeout, "Request timeout");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("PayTR API request cancelled: {Method} {Url}", method.Method, url);
                return (default(TResponse), HttpStatusCode.RequestTimeout, "Request cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in PayTR API request: {Method} {Url}", method.Method, url);
                return (default(TResponse), HttpStatusCode.InternalServerError, $"Unexpected error: {ex.Message}");
            }
        }

        private void LogSecureRequest<TRequest>(HttpMethod method, string url, TRequest requestData)
        {
            if (requestData == null)
            {
                _logger.LogInformation("PayTR API Request: {Method} {Url} (no data)", method.Method, url);
                return;
            }

            // Log request details without sensitive information
            var requestInfo = requestData switch
            {
                PayTRDirectAPIPaymentDTO direct => $"Direct Payment - Amount: {direct.payment_amount}, Merchant: {direct.merchant_id}",
                PayTRCreateLinkAPIPaymentDTO createLink => $"Create Link - Name: {createLink.name}, Price: {createLink.price}",
                PayTRDeleteLinkAPIPaymentDTO deleteLink => $"Delete Link - ID: {deleteLink.id}",
                _ => "Unknown request type"
            };

            _logger.LogInformation("PayTR API Request: {Method} {Url} - {RequestInfo}", method.Method, url, requestInfo);
        }

        private Dictionary<string, string> BuildFormData<TRequest>(TRequest requestData)
        {
            var formData = new Dictionary<string, string>();

            if (requestData == null)
                return formData;

            return requestData switch
            {
                PayTRDirectAPIPaymentDTO direct => BuildDirectPaymentFormData(direct),
                PayTRCreateLinkAPIPaymentDTO createLink => BuildCreateLinkFormData(createLink),
                PayTRDeleteLinkAPIPaymentDTO deleteLink => BuildDeleteLinkFormData(deleteLink),
                _ => BuildGenericFormData(requestData)
            };
        }

        private Dictionary<string, string> BuildDirectPaymentFormData(PayTRDirectAPIPaymentDTO dto)
        {
            return new Dictionary<string, string>
            {
                ["merchant_id"] = dto.merchant_id.ToString(),
                ["paytr_token"] = dto.paytr_token ?? string.Empty,
                ["user_ip"] = dto.user_ip ?? string.Empty,
                ["merchant_oid"] = dto.merchant_oid ?? string.Empty,
                ["email"] = dto.email ?? string.Empty,
                ["payment_type"] = dto.payment_type ?? "card",
                ["payment_amount"] = dto.payment_amount?.ToString("F2", CultureInfo.InvariantCulture) ?? "0",
                ["installment_count"] = dto.installment_count?.ToString() ?? "0",
                ["card_type"] = dto.card_type ?? string.Empty,
                ["currency"] = dto.currency ?? "TL",
                ["client_lang"] = dto.client_lang ?? "tr",
                ["test_mode"] = dto.test_mode ?? "0",
                ["non_3d"] = dto.non_3d ?? "0",
                ["non3d_test_failed"] = dto.non3d_test_failed ?? "1",
                ["cc_owner"] = dto.cc_owner ?? string.Empty,
                ["card_number"] = dto.card_number ?? string.Empty,
                ["expiry_month"] = dto.expiry_month ?? string.Empty,
                ["expiry_year"] = dto.expiry_year ?? string.Empty,
                ["cvv"] = dto.cvv ?? string.Empty,
                ["merchant_ok_url"] = dto.merchant_ok_url ?? string.Empty,
                ["merchant_fail_url"] = dto.merchant_fail_url ?? string.Empty,
                ["user_name"] = dto.user_name ?? string.Empty,
                ["user_address"] = dto.user_address ?? string.Empty,
                ["user_phone"] = dto.user_phone ?? string.Empty,
                ["user_basket"] = dto.user_basket ?? string.Empty,
                ["debug_on"] = dto.debug_on.ToString(),
                ["sync_mode"] = dto.sync_mode.ToString()
            };
        }

        private Dictionary<string, string> BuildCreateLinkFormData(PayTRCreateLinkAPIPaymentDTO dto)
        {
            return new Dictionary<string, string>
            {
                ["merchant_id"] = dto.merchant_id.ToString(),
                ["name"] = dto.name ?? string.Empty,
                ["price"] = dto.price ?? string.Empty,
                ["currency"] = dto.currency ?? "TL",
                ["max_installment"] = dto.max_installment ?? "1",
                ["lang"] = dto.lang ?? "tr",
                ["get_qr"] = dto.get_qr ?? "0",
                ["link_type"] = dto.link_type ?? "product",
                ["paytr_token"] = dto.paytr_token ?? string.Empty,
                ["min_count"] = dto.min_count ?? "0",
                ["max_count"] = dto.max_count ?? "0",
                ["expiry_date"] = dto.expiry_date ?? string.Empty,
                ["callback_link"] = dto.callback_link ?? string.Empty,
                ["callback_id"] = dto.callback_id ?? string.Empty,
                ["debug_on"] = dto.debug_on.ToString()
            };
        }

        private Dictionary<string, string> BuildDeleteLinkFormData(PayTRDeleteLinkAPIPaymentDTO dto)
        {
            return new Dictionary<string, string>
            {
                ["merchant_id"] = dto.merchant_id.ToString(),
                ["id"] = dto.id.ToString(),
                ["paytr_token"] = dto.paytr_token ?? string.Empty,
                ["debug_on"] = dto.debug_on.ToString()
            };
        }

        private Dictionary<string, string> BuildGenericFormData<T>(T obj)
        {
            var formData = new Dictionary<string, string>();
            
            if (obj == null)
                return formData;

            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            
            foreach (var property in properties)
            {
                var value = property.GetValue(obj);
                if (value != null)
                {
                    formData[property.Name.ToLowerInvariant()] = value.ToString() ?? string.Empty;
                }
            }

            return formData;
        }
    }
} 