namespace QR_Menu.PayTRService.Models
{
    public class PayTRCreatePaymentLinkDTO
    {
        public string? Products { get; set; }
        public double? TotalPrice { get; set; }
        public int Installment { get; set; }
        public int StockQuantity { get; set; }
        public bool CreateQR { get; set; }
        public DateTime ExpiryDate { get; set; }
    }
}
