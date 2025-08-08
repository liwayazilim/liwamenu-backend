namespace QR_Menu.PayTRService.Models
{
    public class PayTRCreateLinkAPIPaymentDTO
    {
        public int merchant_id { get; set; }
        public string? name { get; set; }
        public string? price { get; set; }
        public string? currency { get; set; } = "TL";
        public string? max_installment { get; set; } = "1";
        public string? lang { get; set; } = "tr";
        public string? get_qr { get; set; } = "0";
        public string? link_type { get; set; } = "product";
        public string? paytr_token { get; set; }
        public string? max_count { get; set; } = "0";
        public string? min_count { get; set; } = "0";
        public string? expiry_date { get; set; }
        public string? callback_link { get; set; } = "https://api.pentegrasyon.net/api/v1/PayTR/Callback";
        public string? callback_id { get; set; }
        //TODO: Canlıda burası 0 olacak.
        public int debug_on { get; set; } = 0;
    }
}
