namespace QR_Menu.PayTRService.Models
{
    public class PayTRDirectAPITokenDTO
    {     
        public int merchant_id { get; set; }
        public string? merchant_key { get; set; }
        public string? merchant_salt { get; set; }
        public string? user_ip { get; set; }
        public string? merchant_oid { get; set; }
        public string? email { get; set; }
        public double? payment_amount { get; set; }
        public string? payment_type { get; set; } = "card";
        public int installment_count { get; set; } = 0;
        public string? currency { get; set; } = "TL";
        //TODO: Canlıda burası 0 olacak.
        public string? test_mode { get; set; } = "0";
        public string? non_3d { get; set; } = "0";
        public int request_exp_date { get; set; }
    }
}
