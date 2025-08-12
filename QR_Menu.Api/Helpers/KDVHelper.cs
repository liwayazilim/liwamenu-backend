namespace QR_Menu.Api.Helpers;

    public class KDVHelper
    {
        public static double CalculateKDV(double price, double kdv, bool useKDV)
        {
            double basePrice = price;
            double totalPrice = useKDV
                ? basePrice + (basePrice * kdv / 100)
                : basePrice;

            return totalPrice;
        }
    }

