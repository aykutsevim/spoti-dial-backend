using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace SpotiDialBackend.Services;

public class ImageProcessingService
{
    private readonly ILogger<ImageProcessingService> _logger;
    private const int TargetWidth = 240;  // M5Dial display width
    private const int TargetHeight = 240; // M5Dial display height

    public ImageProcessingService(ILogger<ImageProcessingService> logger)
    {
        _logger = logger;
    }

    public async Task<byte[]?> ProcessImageForDeviceAsync(byte[] imageData)
    {
        if (imageData == null || imageData.Length == 0)
        {
            _logger.LogWarning("Empty image data provided");
            return null;
        }

        try
        {
            using var inputStream = new MemoryStream(imageData);
            using var image = await Image.LoadAsync(inputStream);

            // Resize to fit M5Dial display
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(TargetWidth, TargetHeight),
                Mode = ResizeMode.Crop,
                Position = AnchorPositionMode.Center
            }));

            // Save as JPEG with compression
            using var outputStream = new MemoryStream();
            await image.SaveAsJpegAsync(outputStream, new JpegEncoder
            {
                Quality = 80
            });

            var result = outputStream.ToArray();
            _logger.LogInformation("Image processed: {OriginalSize} bytes -> {ProcessedSize} bytes",
                imageData.Length, result.Length);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing image");
            return null;
        }
    }
}
