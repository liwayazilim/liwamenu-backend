using Microsoft.AspNetCore.Http;

namespace QR_Menu.Application.Common;

public interface IImageService
{
    Task<(byte[] imageData, string fileName, string contentType)> ProcessAndStoreImageAsync(IFormFile imageFile, Guid restaurantId, string webRootPath);
    Task<bool> DeleteImageAsync(string fileName, string webRootPath);
    Task<byte[]?> GetImageAsync(string fileName, string webRootPath);
    bool IsValidImageFile(IFormFile file);
    string GetImageUrl(string fileName);
} 