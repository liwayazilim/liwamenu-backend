using QR_Menu.PayTRService.Models;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace QR_Menu.PayTRService.Helpers
{
    public class TokenHasherHelper
    {
        public static string CreatePayTRToken(PayTRDirectAPITokenDTO model)
        {
            // PayTR expects payment_amount in kuruş as integer string in token construction
            var amountKurus = ((int)System.Math.Round((model.payment_amount ?? 0)) ).ToString(CultureInfo.InvariantCulture);
            string concat = string.Concat(model.merchant_id, model.user_ip, model.merchant_oid, model.email, amountKurus, model.payment_type, model.installment_count, model.currency, model.test_mode, model.non_3d, model.merchant_salt);
            HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(model.merchant_key));
            byte[] bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(concat));
            string token = Convert.ToBase64String(bytes);
            return token;
        }

        public static string CreatePayTRToken(PayTRCreateLinkAPITokenDTO model)
        {
            string concat = string.Concat(model.name, model.price, model.currency, model.max_installment, model.link_type, model.lang, model.min_count, model.merchant_salt);
            HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(model.merchant_key));
            byte[] bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(concat));
            string token = Convert.ToBase64String(bytes);
            return token;
        }

        public static string CreatePayTRToken(PayTRDeleteLinkAPITokenDTO model)
        {
            string concat = string.Concat(model.id, model.merchant_id, model.merchant_salt);
            HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(model.merchant_key));
            byte[] bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(concat));
            string token = Convert.ToBase64String(bytes);
            return token;
        }
    }
}
 