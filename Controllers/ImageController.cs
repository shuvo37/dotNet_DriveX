using DriveX.DTOs.Auth;
using DriveX.Services;
using Microsoft.AspNetCore.Mvc;

namespace DriveX.Controllers;

[ApiController]
[Route("api/images")]
public class ImageController : ControllerBase
{
    private readonly IImageService _imageService;
    private readonly IAuthService _authService;

    public ImageController(IImageService imageService, IAuthService authService)
    {
        _imageService = imageService;
        _authService = authService;
    }

    // Same as Spring: the form field is called "file" and the response body is the plain image URL.
    [HttpPost("upload")]
    public async Task<ActionResult<string>> Upload(IFormFile file)
    {
        return Ok(await _imageService.UploadAsync(file));
    }

    // Saves an already uploaded image URL as the user's profile image.
    // Returns a UserResponse instead of Spring's whole User (which held the password hash).
    [HttpPost("upload-image")]
    public async Task<ActionResult<UserResponse>> UploadProfileImage([FromQuery] string imageUrl, [FromQuery] string email)
    {
        return Ok(await _authService.UpdateProfileImageAsync(imageUrl, email));
    }
}
