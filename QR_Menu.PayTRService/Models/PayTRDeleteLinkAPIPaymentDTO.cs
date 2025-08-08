namespace QR_Menu.PayTRService.Models
{
    public class PayTRDeleteLinkAPIPaymentDTO
    {
        public string? id { get; set; }
        public int merchant_id { get; set; }      
        public string? paytr_token { get; set; }   
        //TODO: Canlıda burası 0 olacak.
        public int debug_on { get; set; } = 0;
    }
}
