using Microsoft.EntityFrameworkCore;
using QR_Menu.Domain;
using QR_Menu.Infrastructure;

namespace QR_Menu.Api.Helpers
{
    public class PaymentOrderNumberHelper
    {
        private readonly Random _random;
        private readonly AppDbContext _context;

        public PaymentOrderNumberHelper(
            AppDbContext context)
        {
            _random = new Random();
            _context = context;
        }

        public async Task<string> GetNextOrderNumberAsync()
        {
            int maxAttempts = 10; // Çakışma durumunda kaç defa tekrar denenecek
            for (int i = 0; i < maxAttempts; i++)
            {
                int randomNumber = _random.Next(1, 999999); // 1 ile 999999 arasında rastgele bir sayı üret
                string newOrderNumber = $"SP{randomNumber:D6}"; // Sayıyı 6 basamaklı olacak şekilde formatla

                // Payment ve TemporaryPayment tablolarında mevcut olup olmadığını kontrol et
                var paymentExists = await _context.Payments.AnyAsync(p => p.OrderNumber == newOrderNumber);
                if (!paymentExists)
                    return newOrderNumber;
            }

            // Max deneme sayısı aşıldıysa default değer döndür
            return "SP000000";
        }
    }
}
