using DriveX.DTOs.PasswordReset;

namespace DriveX.Services;

public interface IPasswordResetService
{
    Task<ForgotPasswordResponse> CheckEmailAsync(string email);
    Task<bool> VerifyCodeAsync(string email, string code);
    Task<ResendCodeResponse> ResendCodeAsync(string email);
    Task ResetPasswordAsync(string email, string code, string newPassword);
}
