using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using QR_Menu.PayTRService.Models;

namespace QR_Menu.PayTRService.Services
{
    /// <summary>
    /// Secure PayTR service for handling cryptographic operations and credential management
    /// </summary>
    public interface IPayTRSecurityService
    {
        /// <summary>
        /// Generates a secure PayTR token for direct payment requests
        /// </summary>
        string GenerateDirectPaymentToken(PayTRDirectAPITokenDTO tokenData);
        
        /// <summary>
        /// Generates a secure PayTR token for create link requests
        /// </summary>
        string GenerateCreateLinkToken(PayTRCreateLinkAPITokenDTO tokenData);
        
        /// <summary>
        /// Generates a secure PayTR token for delete link requests
        /// </summary>
        string GenerateDeleteLinkToken(PayTRDeleteLinkAPITokenDTO tokenData);
        
        /// <summary>
        /// Validates a PayTR callback signature
        /// </summary>
        bool ValidateCallbackSignature(string hash, string merchantKey, string merchantSalt, string merchantId, string merchantOid, string status, string totalAmount);
        
        /// <summary>
        /// Validates a PayTR callback hash
        /// </summary>
        bool ValidateCallbackHash(PayTRCallbackDTO callback);
        
        /// <summary>
        /// Creates a secure payment request with proper token generation
        /// </summary>
        PayTRDirectAPIPaymentDTO CreateSecureDirectPaymentRequest(PayTRDirectAPITokenDTO tokenData, string userIp, string merchantOid, string email, decimal amount, string cardNumber, string expiryMonth, string expiryYear, string cvv, string cardOwner);
        
        /// <summary>
        /// Creates a secure async payment request with proper token generation
        /// </summary>
        Task<PayTRDirectAPIPaymentDTO> CreateSecureDirectPaymentRequestAsync(
            string userIp, string merchantOid, string email, decimal amount, 
            string cardNumber, string expiryMonth, string expiryYear, string cvv, string cardOwner,
            string userName, string userAddress, string userPhone, string userBasket);
        
        /// <summary>
        /// Creates a secure link creation request with proper token generation
        /// </summary>
        PayTRCreateLinkAPIPaymentDTO CreateSecureCreateLinkRequest(PayTRCreateLinkAPITokenDTO tokenData, string name, string price, string? callbackLink = null);
        
        /// <summary>
        /// Creates a secure async link creation request
        /// </summary>
        Task<PayTRCreateLinkAPIPaymentDTO> CreateSecureCreateLinkRequestAsync(
            string name, string price, string maxInstallment, string maxCount, 
            DateTime expiryDate, string callbackId, int getQr = 0);
        
        /// <summary>
        /// Creates a secure async link deletion request
        /// </summary>
        Task<PayTRDeleteLinkAPIPaymentDTO> CreateSecureDeleteLinkRequestAsync(string linkId);
    }

    public class PayTRSecurityService : IPayTRSecurityService
    {
        private readonly PayTRConfiguration _configuration;
        private readonly ILogger<PayTRSecurityService> _logger;

        public PayTRSecurityService(IOptions<PayTRConfiguration> configuration, ILogger<PayTRSecurityService> logger)
        {
            _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string GenerateDirectPaymentToken(PayTRDirectAPITokenDTO tokenData)
        {
            try
            {
                // Concatenate fields in the exact order required by PayTR
                var concatenatedString = $"{tokenData.merchant_id}{tokenData.user_ip}{tokenData.merchant_oid}{tokenData.email}{tokenData.payment_amount}{tokenData.payment_type}{tokenData.installment_count}{tokenData.currency}{tokenData.test_mode}{tokenData.non_3d}{_configuration.MerchantKey}{_configuration.MerchantSalt}";
                
                return GenerateHash(concatenatedString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating direct payment token");
                throw new InvalidOperationException("Failed to generate PayTR token", ex);
            }
        }

        public string GenerateCreateLinkToken(PayTRCreateLinkAPITokenDTO tokenData)
        {
            try
            {
                // Concatenate fields in the exact order required by PayTR
                var concatenatedString = $"{tokenData.name}{tokenData.price}{tokenData.currency}{tokenData.max_installment}{tokenData.max_count}{tokenData.link_type}{tokenData.lang}{_configuration.MerchantKey}{_configuration.MerchantSalt}";
                
                return GenerateHash(concatenatedString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating create link token");
                throw new InvalidOperationException("Failed to generate PayTR token", ex);
            }
        }

        public string GenerateDeleteLinkToken(PayTRDeleteLinkAPITokenDTO tokenData)
        {
            try
            {
                // Concatenate fields in the exact order required by PayTR
                var concatenatedString = $"{tokenData.id}{tokenData.merchant_id}{_configuration.MerchantKey}{_configuration.MerchantSalt}";
                
                return GenerateHash(concatenatedString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating delete link token");
                throw new InvalidOperationException("Failed to generate PayTR token", ex);
            }
        }

        public bool ValidateCallbackSignature(string hash, string merchantKey, string merchantSalt, string merchantId, string merchantOid, string status, string totalAmount)
        {
            try
            {
                // Reconstruct the hash using the same algorithm
                var concatenatedString = $"{merchantId}{merchantOid}{status}{totalAmount}{merchantKey}{merchantSalt}";
                var expectedHash = GenerateHash(concatenatedString);
                
                // Use constant-time comparison to prevent timing attacks
                return CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(hash), 
                    Encoding.UTF8.GetBytes(expectedHash));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating callback signature");
                return false;
            }
        }

        public PayTRDirectAPIPaymentDTO CreateSecureDirectPaymentRequest(
            PayTRDirectAPITokenDTO tokenData, 
            string userIp, 
            string merchantOid, 
            string email, 
            decimal amount, 
            string cardNumber, 
            string expiryMonth, 
            string expiryYear, 
            string cvv, 
            string cardOwner)
        {
            // Generate the secure token
            var paytrToken = GenerateDirectPaymentToken(tokenData);
            
            // Mask card number for logging (only show last 4 digits)
            var maskedCardNumber = MaskCardNumber(cardNumber);
            _logger.LogInformation("Creating secure direct payment request for merchant {MerchantId}, amount {Amount}, card ending in {MaskedCard}", 
                tokenData.merchant_id, amount, maskedCardNumber);

            return new PayTRDirectAPIPaymentDTO
            {
                merchant_id = tokenData.merchant_id,
                paytr_token = paytrToken,
                user_ip = userIp,
                merchant_oid = merchantOid,
                email = email,
                payment_type = "card",
                payment_amount = (double)amount, // Convert decimal to double
                installment_count = 0,
                card_type = "1", // 1 for credit card
                currency = "TL",
                client_lang = "tr",
                test_mode = tokenData.test_mode,
                non_3d = "0",
                non3d_test_failed = "1",
                cc_owner = cardOwner,
                card_number = cardNumber,
                expiry_month = expiryMonth,
                expiry_year = expiryYear,
                cvv = cvv,
                merchant_ok_url = _configuration.SuccessUrl ?? "https://yourdomain.com/payment/success",
                merchant_fail_url = _configuration.FailUrl ?? "https://yourdomain.com/payment/fail",
                user_name = cardOwner,
                user_address = "",
                user_phone = "",
                user_basket = "[]", // Empty basket for direct payment
                debug_on = 1,
                sync_mode = 1
            };
        }

        public PayTRCreateLinkAPIPaymentDTO CreateSecureCreateLinkRequest(
            PayTRCreateLinkAPITokenDTO tokenData, 
            string name, 
            string price, 
            string? callbackLink = null)
        {
            // Generate the secure token
            var paytrToken = GenerateCreateLinkToken(tokenData);
            
            _logger.LogInformation("Creating secure payment link for name {Name}, price {Price}", name, price);

            return new PayTRCreateLinkAPIPaymentDTO
            {
                merchant_id = _configuration.MerchantId, // Use from configuration
                paytr_token = paytrToken,
                name = name,
                price = price,
                currency = "TL",
                max_installment = "1",
                lang = "tr",
                get_qr = "0",
                link_type = "product",
                min_count = "0",
                max_count = "0",
                expiry_date = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd"), // 7 days expiry
                callback_link = callbackLink ?? _configuration.CallbackUrl ?? "https://yourdomain.com/payment/callback",
                callback_id = Guid.NewGuid().ToString(),
                debug_on = 1
            };
        }

        private string GenerateHash(string input)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// Mask sensitive data (only show last 4 digits)
        /// </summary>
        private string MaskCardNumber(string cardNumber)
        {
            if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 4)
                return "****";
            
            return $"****{cardNumber.Substring(cardNumber.Length - 4)}";
        }

        public bool ValidateCallbackHash(PayTRCallbackDTO callback)
        {
            if (callback?.Hash == null) return false;
            
            return ValidateCallbackSignature(
                callback.Hash,
                _configuration.MerchantKey,
                _configuration.MerchantSalt,
                callback.MerchantId.ToString(),
                callback.MerchantOid ?? "",
                callback.Status ?? "",
                callback.TotalAmount.ToString("F2")
            );
        }

        public async Task<PayTRDirectAPIPaymentDTO> CreateSecureDirectPaymentRequestAsync(
            string userIp, string merchantOid, string email, decimal amount, 
            string cardNumber, string expiryMonth, string expiryYear, string cvv, string cardOwner,
            string userName, string userAddress, string userPhone, string userBasket)
        {
            await Task.Delay(0); // Make it truly async if needed
            
            var tokenData = new PayTRDirectAPITokenDTO
            {
                merchant_id = _configuration.MerchantId,
                merchant_key = _configuration.MerchantKey,
                merchant_salt = _configuration.MerchantSalt,
                user_ip = userIp,
                merchant_oid = merchantOid,
                email = email,
                payment_amount = (double)amount,
                payment_type = "card",
                installment_count = 0,
                currency = "TL",
                test_mode = _configuration.TestMode ? "1" : "0",
                non_3d = "0"
            };

            var token = GenerateDirectPaymentToken(tokenData);
            
            return new PayTRDirectAPIPaymentDTO
            {
                merchant_id = _configuration.MerchantId,
                paytr_token = token,
                user_ip = userIp,
                merchant_oid = merchantOid,
                email = email,
                payment_amount = (double)amount,
                payment_type = "card",
                installment_count = 0,
                currency = "TL",
                test_mode = _configuration.TestMode ? "1" : "0",
                non_3d = "0",
                cc_owner = cardOwner,
                card_number = cardNumber,
                expiry_month = expiryMonth,
                expiry_year = expiryYear,
                cvv = cvv,
                user_name = userName,
                user_address = userAddress,
                user_phone = userPhone,
                user_basket = userBasket,
                merchant_ok_url = _configuration.SuccessUrl ?? "https://yourdomain.com/payment-success",
                merchant_fail_url = _configuration.FailUrl ?? "https://yourdomain.com/payment-failure"
            };
        }

        public async Task<PayTRCreateLinkAPIPaymentDTO> CreateSecureCreateLinkRequestAsync(
            string name, string price, string maxInstallment, string maxCount, 
            DateTime expiryDate, string callbackId, int getQr = 0)
        {
            await Task.Delay(0); // Make it truly async if needed
            
            var tokenData = new PayTRCreateLinkAPITokenDTO
            {
                name = name,
                price = price,
                max_installment = maxInstallment,
                max_count = maxCount,
                merchant_key = _configuration.MerchantKey,
                merchant_salt = _configuration.MerchantSalt
            };

            var token = GenerateCreateLinkToken(tokenData);
            
            return new PayTRCreateLinkAPIPaymentDTO
            {
                merchant_id = _configuration.MerchantId,
                paytr_token = token,
                name = name,
                price = price,
                currency = "TL",
                max_installment = maxInstallment,
                max_count = maxCount,
                lang = "tr",
                get_qr = getQr.ToString(),
                link_type = "product",
                expiry_date = expiryDate.ToString("yyyy-MM-dd HH:mm:ss"),
                callback_id = callbackId
            };
        }

        public async Task<PayTRDeleteLinkAPIPaymentDTO> CreateSecureDeleteLinkRequestAsync(string linkId)
        {
            await Task.Delay(0); // Make it truly async if needed
            
            if (!int.TryParse(linkId, out int id))
                throw new ArgumentException("Invalid link ID format", nameof(linkId));
            
            var tokenData = new PayTRDeleteLinkAPITokenDTO
            {
                id = id,
                merchant_id = _configuration.MerchantId,
                merchant_key = _configuration.MerchantKey,
                merchant_salt = _configuration.MerchantSalt
            };

            var token = GenerateDeleteLinkToken(tokenData);
            
            return new PayTRDeleteLinkAPIPaymentDTO
            {
                id = id,
                merchant_id = _configuration.MerchantId,
                paytr_token = token
            };
        }
    }
} 