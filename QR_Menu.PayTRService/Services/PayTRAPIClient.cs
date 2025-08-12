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
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        private static void AddIfNotNull(Dictionary<string, string> dict, string key, string? value)
        {
            if (!string.IsNullOrEmpty(value)) dict[key] = value;
        }

        public async Task<(TResponse, HttpStatusCode)> SendRequest<TRequest, TResponse>(
            HttpMethod method,
            string url,
            TRequest requestData,
            string token = "")
        {
            try
            {
                var request = new HttpRequestMessage(method, url);

                if (string.IsNullOrEmpty(token) && requestData != null)
                {
                    if (requestData is PayTRDirectAPIPaymentDTO pay)
                    {
                        var formData = new Dictionary<string, string>();
                        AddIfNotNull(formData, "merchant_id", pay.merchant_id.ToString());
                        AddIfNotNull(formData, "paytr_token", pay.paytr_token);
                        AddIfNotNull(formData, "user_ip", pay.user_ip);
                        AddIfNotNull(formData, "merchant_oid", pay.merchant_oid);
                        AddIfNotNull(formData, "email", pay.email);
                        AddIfNotNull(formData, "payment_type", pay.payment_type);
                        // payment_amount must be kuruş integer
                        var amountKurus = ((int)System.Math.Round((pay.payment_amount ?? 0))).ToString(CultureInfo.InvariantCulture);
                        AddIfNotNull(formData, "payment_amount", amountKurus);
                        AddIfNotNull(formData, "installment_count", pay.installment_count?.ToString());
                        AddIfNotNull(formData, "card_type", pay.card_type);
                        AddIfNotNull(formData, "currency", pay.currency);
                        AddIfNotNull(formData, "client_lang", pay.client_lang);
                        AddIfNotNull(formData, "test_mode", pay.test_mode);
                        AddIfNotNull(formData, "non_3d", pay.non_3d);
                        AddIfNotNull(formData, "non3d_test_failed", pay.non3d_test_failed);
                        AddIfNotNull(formData, "cc_owner", pay.cc_owner);
                        AddIfNotNull(formData, "card_number", pay.card_number);
                        AddIfNotNull(formData, "expiry_month", pay.expiry_month);
                        AddIfNotNull(formData, "expiry_year", pay.expiry_year);
                        AddIfNotNull(formData, "cvv", pay.cvv);
                        AddIfNotNull(formData, "merchant_ok_url", pay.merchant_ok_url);
                        AddIfNotNull(formData, "merchant_fail_url", pay.merchant_fail_url);
                        AddIfNotNull(formData, "user_name", pay.user_name);
                        AddIfNotNull(formData, "user_address", pay.user_address);
                        AddIfNotNull(formData, "user_phone", pay.user_phone);
                        AddIfNotNull(formData, "user_basket", pay.user_basket);
                        AddIfNotNull(formData, "debug_on", pay.debug_on.ToString());
                        AddIfNotNull(formData, "sync_mode", pay.sync_mode.ToString());
                        request.Content = new FormUrlEncodedContent(formData);
                    }
                    else if (requestData is PayTRCreateLinkAPIPaymentDTO link)
                    {
                        var formData = new Dictionary<string, string>();
                        AddIfNotNull(formData, "merchant_id", link.merchant_id.ToString());
                        AddIfNotNull(formData, "name", link.name);
                        AddIfNotNull(formData, "price", link.price);
                        AddIfNotNull(formData, "currency", link.currency);
                        AddIfNotNull(formData, "max_installment", link.max_installment);
                        AddIfNotNull(formData, "lang", link.lang);
                        AddIfNotNull(formData, "get_qr", link.get_qr);
                        AddIfNotNull(formData, "link_type", link.link_type);
                        AddIfNotNull(formData, "paytr_token", link.paytr_token);
                        AddIfNotNull(formData, "min_count", link.min_count);
                        AddIfNotNull(formData, "expiry_date", link.expiry_date);
                        AddIfNotNull(formData, "callback_link", link.callback_link);
                        AddIfNotNull(formData, "callback_id", link.callback_id);
                        AddIfNotNull(formData, "debug_on", link.debug_on.ToString());
                        request.Content = new FormUrlEncodedContent(formData);
                    }
                    else if (requestData is PayTRDeleteLinkAPIPaymentDTO del)
                    {
                        var formData = new Dictionary<string, string>();
                        AddIfNotNull(formData, "merchant_id", del.merchant_id.ToString());
                        AddIfNotNull(formData, "id", del.id?.ToString());
                        AddIfNotNull(formData, "paytr_token", del.paytr_token);
                        AddIfNotNull(formData, "debug_on", del.debug_on.ToString());
                        request.Content = new FormUrlEncodedContent(formData);
                    }
                }

                var response = await _httpClient.SendAsync(request);
                string responseContent = await response.Content.ReadAsStringAsync();

                if (typeof(TResponse) == typeof(string))
                    return ((TResponse)(object)responseContent, response.StatusCode);

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return (JsonSerializer.Deserialize<TResponse>(responseContent, options)!, response.StatusCode);
            }
            catch
            {
                return (default(TResponse)!, HttpStatusCode.InternalServerError);
            }
        }
    }
}