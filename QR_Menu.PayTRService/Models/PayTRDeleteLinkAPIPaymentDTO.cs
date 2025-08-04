namespace QR_Menu.PayTRService.Models
{
    public class PayTRDeleteLinkAPIPaymentDTO
    {
        public int merchant_id { get; set; }
        public int id { get; set; }
        public string? paytr_token { get; set; }
        public int debug_on { get; set; } = 0;
    }
} 