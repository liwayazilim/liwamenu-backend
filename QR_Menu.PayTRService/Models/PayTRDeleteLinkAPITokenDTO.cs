namespace QR_Menu.PayTRService.Models
{
    public class PayTRDeleteLinkAPITokenDTO
    {
        public int id { get; set; }
        public int merchant_id { get; set; }
        public string? merchant_key { get; set; }
        public string? merchant_salt { get; set; }
    }
} 