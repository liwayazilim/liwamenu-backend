using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Bmp;

namespace QR_Menu.Application.Common;

public class ImageService : IImageService
{
    private readonly ILogger<ImageService> _logger;
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
    private readonly int _maxFileSizeInMB = 5;
    private readonly int _maxImageWidth = 800;
    private readonly int _maxImageHeight = 600;

    public ImageService(ILogger<ImageService> logger)
    {
        _logger = logger;
    }

    public async Task<(byte[] imageData, string fileName, string contentType)> ProcessAndStoreImageAsync(IFormFile imageFile, Guid restaurantId, string webRootPath)
    {
        if (imageFile == null || imageFile.Length == 0)
            throw new ArgumentException("Image file is required");

        if (!IsValidImageFile(imageFile))
            throw new ArgumentException("Invalid image file format or size");

        if (string.IsNullOrEmpty(webRootPath))
            throw new ArgumentException("Web root path cannot be null or empty");

        // Generate unique filename
        var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
        var fileName = $"{restaurantId}{fileExtension}";
        var imagesPath = Path.Combine(webRootPath, "images", "restaurants");
        var filePath = Path.Combine(imagesPath, fileName);

        // Ensure directory exists
        if (!Directory.Exists(imagesPath))
        {
            Directory.CreateDirectory(imagesPath);
        }

        // Process and resize image
        using var imageStream = imageFile.OpenReadStream();
        using var originalImage = await Image.LoadAsync(imageStream);
        
        // Resize image if needed
        var resizedImage = ResizeImage(originalImage, _maxImageWidth, _maxImageHeight);
        
        // Save to file system
        using var fileStream = new FileStream(filePath, FileMode.Create);
        await resizedImage.SaveAsync(fileStream, GetImageFormat(fileExtension));
        
        // Convert to byte array for database storage
        using var memoryStream = new MemoryStream();
        await resizedImage.SaveAsync(memoryStream, GetImageFormat(fileExtension));
        var imageData = memoryStream.ToArray();

        _logger.LogInformation("Image processed and stored for restaurant {RestaurantId}: {FileName}", restaurantId, fileName);
        
        return (imageData, fileName, imageFile.ContentType);
    }

    public async Task<bool> DeleteImageAsync(string fileName, string webRootPath)
    {
        if (string.IsNullOrEmpty(fileName))
            return false;

        if (string.IsNullOrEmpty(webRootPath))
            return false;

        var filePath = Path.Combine(webRootPath, "images", "restaurants", fileName);
        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
                _logger.LogInformation("Image deleted: {FileName}", fileName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image: {FileName}", fileName);
                return false;
            }
        }
        return false;
    }

    public async Task<byte[]?> GetImageAsync(string fileName, string webRootPath)
    {
        if (string.IsNullOrEmpty(fileName))
            return null;

        if (string.IsNullOrEmpty(webRootPath))
            return null;

        var filePath = Path.Combine(webRootPath, "images", "restaurants", fileName);
        if (File.Exists(filePath))
        {
            try
            {
                return await File.ReadAllBytesAsync(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading image: {FileName}", fileName);
                return null;
            }
        }
        return null;
    }

    public bool IsValidImageFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return false;

        // Check file size (5MB limit)
        if (file.Length > _maxFileSizeInMB * 1024 * 1024)
            return false;

        // Check file extension
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
            return false;

        // Check content type
        var allowedContentTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/bmp" };
        if (!allowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
            return false;

        return true;
    }

    public string GetImageUrl(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return string.Empty;
            
        return $"/images/restaurants/{fileName}";
    }

    private Image ResizeImage(Image image, int maxWidth, int maxHeight)
    {
        var ratioX = (double)maxWidth / image.Width;
        var ratioY = (double)maxHeight / image.Height;
        var ratio = Math.Min(ratioX, ratioY);

        var newWidth = (int)(image.Width * ratio);
        var newHeight = (int)(image.Height * ratio);

        // Clone the image and resize it
        var resizedImage = image.Clone(ctx => ctx
            .Resize(newWidth, newHeight));

        return resizedImage;
    }

    private IImageFormat GetImageFormat(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => JpegFormat.Instance,
            ".png" => PngFormat.Instance,
            ".gif" => GifFormat.Instance,
            ".bmp" => BmpFormat.Instance,
            _ => JpegFormat.Instance
        };
    }
} 