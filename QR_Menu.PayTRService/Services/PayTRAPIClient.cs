using QR_Menu.PayTRService.Models;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace QR_Menu.PayTRService.Services
{
    public class PayTRAPIClient
    {
        private HttpClient _httpClient = new HttpClient();

        public PayTRAPIClient()
        {
            _httpClient.DefaultRequestHeaders.Clear(); // Header'ları temizleyin
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public async Task<(TResponse, HttpStatusCode)> SendRequest<TRequest, TResponse>(
            HttpMethod method,
            string url,
            TRequest requestData,
            string token = "")
        {
            try
            {
                Console.WriteLine($"[PayTR]: URL: {url}, Method: {method.Method}");

                // JSON verisini serileştir
                string jsonData = JsonSerializer.Serialize(requestData);
                var request = new HttpRequestMessage(method, url);

                if (string.IsNullOrEmpty(token) && requestData != null)
                {
                    // Eğer requestData PayTRPaymentDTO ise işleme al
                    if (requestData is PayTRDirectAPIPaymentDTO payTRDirectAPIPaymentDto)
                    {
                        var formData = new Dictionary<string, string>
                        {
                            { "merchant_id", payTRDirectAPIPaymentDto.merchant_id.ToString() },
                            { "paytr_token", payTRDirectAPIPaymentDto.paytr_token },
                            { "user_ip", payTRDirectAPIPaymentDto.user_ip },
                            { "merchant_oid", payTRDirectAPIPaymentDto.merchant_oid },
                            { "email", payTRDirectAPIPaymentDto.email },
                            { "payment_type", payTRDirectAPIPaymentDto.payment_type },
                            { "payment_amount", string.Format(CultureInfo.InvariantCulture, "{0:F2}", payTRDirectAPIPaymentDto.payment_amount) },
                            { "installment_count", payTRDirectAPIPaymentDto.installment_count.ToString() },
                            { "card_type", payTRDirectAPIPaymentDto.card_type },
                            { "currency", payTRDirectAPIPaymentDto.currency },
                            { "client_lang", payTRDirectAPIPaymentDto.client_lang },
                            { "test_mode", payTRDirectAPIPaymentDto.test_mode },
                            { "non_3d", payTRDirectAPIPaymentDto.non_3d },
                            { "non3d_test_failed", payTRDirectAPIPaymentDto.non3d_test_failed },
                            { "cc_owner", payTRDirectAPIPaymentDto.cc_owner },
                            { "card_number", payTRDirectAPIPaymentDto.card_number },
                            { "expiry_month", payTRDirectAPIPaymentDto.expiry_month },
                            { "expiry_year", payTRDirectAPIPaymentDto.expiry_year },
                            { "cvv", payTRDirectAPIPaymentDto.cvv },
                            { "merchant_ok_url", payTRDirectAPIPaymentDto.merchant_ok_url },
                            { "merchant_fail_url", payTRDirectAPIPaymentDto.merchant_fail_url },
                            { "user_name", payTRDirectAPIPaymentDto.user_name },
                            { "user_address", payTRDirectAPIPaymentDto.user_address },
                            { "user_phone", payTRDirectAPIPaymentDto.user_phone },
                            { "user_basket", payTRDirectAPIPaymentDto.user_basket },
                            { "debug_on", payTRDirectAPIPaymentDto.debug_on.ToString() },
                            { "sync_mode", payTRDirectAPIPaymentDto.sync_mode.ToString() }
                        };

                        request.Content = new FormUrlEncodedContent(formData);
                    }
                    else if (requestData is PayTRCreateLinkAPIPaymentDTO payTRCreateLinkAPIPaymentDto)
                    {
                        var formData = new Dictionary<string, string>
                        {
                            { "merchant_id", payTRCreateLinkAPIPaymentDto.merchant_id.ToString() },
                            { "name", payTRCreateLinkAPIPaymentDto.name },
                            { "price", payTRCreateLinkAPIPaymentDto.price },
                            { "currency", payTRCreateLinkAPIPaymentDto.currency },
                            { "max_installment", payTRCreateLinkAPIPaymentDto.max_installment },
                            { "lang", payTRCreateLinkAPIPaymentDto.lang },
                            { "get_qr", payTRCreateLinkAPIPaymentDto.get_qr },
                            { "link_type", payTRCreateLinkAPIPaymentDto.link_type },
                            { "paytr_token", payTRCreateLinkAPIPaymentDto.paytr_token },
                            { "min_count", payTRCreateLinkAPIPaymentDto.min_count },
                            { "expiry_date", payTRCreateLinkAPIPaymentDto.expiry_date },
                            { "callback_link", payTRCreateLinkAPIPaymentDto.callback_link },
                            { "callback_id", payTRCreateLinkAPIPaymentDto.callback_id },
                            { "debug_on", payTRCreateLinkAPIPaymentDto.debug_on.ToString() },
                        };

                        request.Content = new FormUrlEncodedContent(formData);
                    }
                    else if (requestData is PayTRDeleteLinkAPIPaymentDTO payTRDeleteLinkAPIPaymentDto)
                    {
                        var formData = new Dictionary<string, string>
                        {
                            { "merchant_id", payTRDeleteLinkAPIPaymentDto.merchant_id.ToString() },
                            { "id", payTRDeleteLinkAPIPaymentDto.id.ToString() },
                            { "paytr_token", payTRDeleteLinkAPIPaymentDto.paytr_token },
                            { "debug_on", payTRDeleteLinkAPIPaymentDto.debug_on.ToString() },
                        };

                        request.Content = new FormUrlEncodedContent(formData);
                    }
                }

                // İsteği gönder ve yanıtı al
                var response = await _httpClient.SendAsync(request);
                string responseContent = await response.Content.ReadAsStringAsync();

                // Yanıt durumunu kontrol et ve logla
                if (!response.IsSuccessStatusCode)
                    Console.WriteLine($"[PayTR]: Request failed. Status Code: {response.StatusCode}, Reason: {response.ReasonPhrase}, Content: {responseContent}");
                else
                    Console.WriteLine($"[PayTR]: Request made successfully. Content: {responseContent}");

                if (typeof(TResponse) == typeof(string))
                    return ((TResponse)(object)responseContent, response.StatusCode);

                // Yanıt verisi JSON'dan dönüştürülerek döndürülüyor
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                return (JsonSerializer.Deserialize<TResponse>(responseContent, options), response.StatusCode);
            }
            catch (Exception exception)
            {
                Console.WriteLine($"[PayTR]: Error occurred: {exception}");
                return (default(TResponse), HttpStatusCode.InternalServerError);
            }
        }
    }
}