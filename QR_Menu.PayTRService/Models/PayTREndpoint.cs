namespace QR_Menu.PayTRService.Models
{
    public class PayTREndpoint
    {
        private static string BaseURL = "https://www.paytr.com/";

        public static string Pay => BaseURL + "odeme";
        public static string CreateLink => BaseURL + "odeme/api/link/create";
        public static string DeleteLink => BaseURL + "odeme/api/link/delete";
    }
}
