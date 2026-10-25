namespace DriveX.Services;

public interface IImageService
{
    // Uploads the file to Cloudinary and returns its public URL.
    Task<string> UploadAsync(IFormFile file);
}
