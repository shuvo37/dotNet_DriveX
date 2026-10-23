using DriveX.DTOs.PasswordReset;
using DriveX.Services;
using Microsoft.AspNetCore.Mvc;

namespace DriveX.Controllers;

[ApiController]
[Route("api/auth/forgot-password")]
public class PasswordResetController : ControllerBase
{
    private readonly IPasswordResetService _passwordResetService;

    public PasswordResetController(IPasswordResetService passwordResetService)
    {
        _passwordResetService = passwordResetService;
    }

    [HttpPost("check-email")]
    public async Task<ActionResult<ForgotPasswordResponse>> CheckEmail(EmailRequest request)
    {
        return Ok(await _passwordResetService.CheckEmailAsync(request.Email));
    }

    [HttpPost("verify-code")]
    public async Task<ActionResult<bool>> VerifyCode(VerifyCodeRequest request)
    {
        return Ok(await _passwordResetService.VerifyCodeAsync(request.Email, request.Code));
    }

    [HttpPost("resend-code")]
    public async Task<ActionResult<ResendCodeResponse>> ResendCode(EmailRequest request)
    {
        return Ok(await _passwordResetService.ResendCodeAsync(request.Email));
    }

    [HttpPost("reset")]
    public async Task<ActionResult<bool>> Reset(ResetPasswordRequest request)
    {
        await _passwordResetService.ResetPasswordAsync(request.Email, request.Code, request.NewPassword);
        return Ok(true);
    }
}
