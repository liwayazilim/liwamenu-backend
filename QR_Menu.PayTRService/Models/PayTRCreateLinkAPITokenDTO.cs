namespace QR_Menu.PayTRService.Models
{
    public class PayTRCreateLinkAPITokenDTO
    {
        public string? name { get; set; }
        public string? price { get; set; }
        public string? currency { get; set; } = "TL";
        public string? max_installment { get; set; } = "1";
        public string? max_count { get; set; } = "0";
        public string? min_count { get; set; } = "0";
        public string? link_type { get; set; } = "product";
        public string? lang { get; set; } = "tr";
        public string? merchant_key { get; set; }
        public string? merchant_salt { get; set; }
    }
} 