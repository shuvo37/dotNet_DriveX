using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DriveX.Exceptions;
using DriveX.Options;
using Microsoft.Extensions.Options;

namespace DriveX.Services;

public class ImageService : IImageService
{
    private readonly Cloudinary _cloudinary;

    public ImageService(IOptions<CloudinarySettings> options)
    {
        var settings = options.Value;
        _cloudinary = new Cloudinary(new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret));
    }

    public async Task<string> UploadAsync(IFormFile file)
    {
        await using var stream = file.OpenReadStream();

        var result = await _cloudinary.UploadAsync(new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream)
        });

        if (result.Error is not null)
        {
            throw new AppException($"Image upload failed: {result.Error.Message}");
        }

        // Spring returned Cloudinary's "url" field (http), so this does the same.
        return result.Url.ToString();
    }
}
