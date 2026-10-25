using DriveX.DTOs.Auth;

namespace DriveX.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);
    Task<LoginResponse> CompleteProfileAsync(CompleteProfileRequest request, string email);
    Task<GoogleLoginResult> GoogleLoginAsync(GoogleProfile profile);
    Task<UserResponse> GetByEmailAsync(string email);
    Task<UserResponse> UpdateRoleAsync(string email, string role);
    Task<UserResponse> UpdateProfileImageAsync(string imageUrl, string email);
}
