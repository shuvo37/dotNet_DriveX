using DriveX.DTOs.Auth;

namespace DriveX.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);
    Task<LoginResponse> CompleteProfileAsync(CompleteProfileRequest request, string email);
    Task<GoogleLoginResult> GoogleLoginAsync(GoogleProfile profile);
}
