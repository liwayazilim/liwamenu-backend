namespace QR_Menu.PayTRService.Models
{
    public class PayTRCreateLinkAPIPaymentResponseDTO
    {
        public string? status { get; set; }
        public string? id { get; set; }
        public string? link { get; set; }
        public string? base64_qr { get; set; }
        public string? qr_link { get; set; }
        public string? err_msg { get; set; }
        public string? err_no { get; set; }
    }
} 