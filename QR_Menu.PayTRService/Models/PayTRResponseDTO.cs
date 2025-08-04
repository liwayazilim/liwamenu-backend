namespace QR_Menu.PayTRService.Models
{
    public class PayTRResponseDTO
    {
        public bool Success { get; set; }
        public string? Status { get; set; }
        public string? Reason { get; set; }
        public string? ReasonCode { get; set; }
        public string? Token { get; set; }
        public string? PostUrl { get; set; }
        public string? TransactionId { get; set; }
        public string? LinkId { get; set; }
        public string? LinkUrl { get; set; }
        public string? QrCodeUrl { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ErrorCode { get; set; }
    }
} 