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
            string concat = string.Concat(model.merchant_id, model.user_ip, model.merchant_oid, model.email, string.Format(CultureInfo.InvariantCulture, "{0:F2}", model.payment_amount), model.payment_type, model.installment_count, model.currency, model.test_mode, model.non_3d, model.merchant_salt);
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
 