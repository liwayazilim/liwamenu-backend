namespace QR_Menu.PayTRService.Models
{
    public class PayTRDirectAPIPaymentDTO
    {
        public int merchant_id { get; set; }
        public string? paytr_token { get; set; }
        public string? user_ip { get; set; }
        public string? merchant_oid { get; set; }
        public string? email { get; set; }
        public string? payment_type { get; set; } = "card";
        public double? payment_amount { get; set; }
        public int? installment_count { get; set; } = 0;
        public string? card_type { get; set; }
        public string? currency { get; set; } = "TL";
        public string? client_lang { get; set; } = "tr";
        public string? test_mode { get; set; } = "0";
        public string? non_3d { get; set; } = "0";
        public string? non3d_test_failed { get; set; } = "1";
        public string? cc_owner { get; set; }
        public string? card_number { get; set; }
        public string? expiry_month { get; set; }
        public string? expiry_year { get; set; }
        public string? cvv { get; set; }
        public string? merchant_ok_url { get; set; } = "https://yourdomain.com/payment-success";
        public string? merchant_fail_url { get; set; } = "https://yourdomain.com/payment-failure";
        public string? user_name { get; set; }
        public string? user_address { get; set; }
        public string? user_phone { get; set; }
        public string? user_basket { get; set; }
        public int debug_on { get; set; } = 0;
        public int sync_mode { get; set; }
    }
} 