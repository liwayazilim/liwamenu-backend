namespace QR_Menu.PayTRService.Models
{
    public class PayTRUserBasketForNewLicenseDTO
    {
        public string? restaurantId { get; set; }
        public List<string>? licensePackageIds { get; set; }
    }
}
