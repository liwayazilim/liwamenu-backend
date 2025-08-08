namespace QR_Menu.PayTRService.Models
{
    public class PayTRCreateLinkAPIPaymentResponseDTO
    {
        public string? status { get; set; }
        public string? reason { get; set; }
        public string? id { get; set; }
        public string? link { get; set; }
        public string? base64_qr { get; set; }
    }
}
