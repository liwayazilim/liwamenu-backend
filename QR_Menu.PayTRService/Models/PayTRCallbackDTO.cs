namespace QR_Menu.PayTRService.Models
{
    public class PayTRCallbackDTO
    {
        public string? Hash { get; set; }
        public string? CallbackId { get; set; }
        public string? MerchantOid { get; set; }
        public string? Status { get; set; }
        public decimal TotalAmount { get; set; }
        public int FailedReasonCode { get; set; }
        public string? FailedReasonMsg { get; set; }
        public string? PaymentType { get; set; }
        public int MerchantId { get; set; }
        public bool TestMode { get; set; }
    }
} 