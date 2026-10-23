using System.Security.Claims;
using DriveX.Constants;
using DriveX.DTOs.Auth;
using DriveX.Exceptions;
using DriveX.Options;
using DriveX.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DriveX.Controllers;

// Google login, with the same URLs the Spring app used, so Login.jsx needs no change.
[ApiController]
public class GoogleAuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IAuthenticationSchemeProvider _schemeProvider;
    private readonly FrontendSettings _frontend;
    private readonly ILogger<GoogleAuthController> _logger;

    public GoogleAuthController(
        IAuthService authService,
        IAuthenticationSchemeProvider schemeProvider,
        IOptions<FrontendSettings> frontend,
        ILogger<GoogleAuthController> logger)
    {
        _authService = authService;
        _schemeProvider = schemeProvider;
        _frontend = frontend.Value;
        _logger = logger;
    }

    // Step 1: Login.jsx sends the browser here. We forward it to Google.
    [HttpGet("/oauth2/authorization/google")]
    public async Task<IActionResult> StartGoogleLogin()
    {
        if (await _schemeProvider.GetSchemeAsync(GoogleDefaults.AuthenticationScheme) is null)
        {
            throw new AppException("Google login is not configured on the server");
        }

        var properties = new AuthenticationProperties { RedirectUri = "/oauth2/google/complete" };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    // Step 2: Google sends the user back (handled by the Google handler), which then lands here.
    [HttpGet("/oauth2/google/complete")]
    public async Task<IActionResult> CompleteGoogleLogin()
    {
        var frontendUrl = _frontend.BaseUrl.TrimEnd('/');

        var result = await HttpContext.AuthenticateAsync(AuthSchemes.External);
        if (!result.Succeeded || result.Principal is null)
        {
            return Redirect($"{frontendUrl}/?error=google_login_failed");
        }

        var principal = result.Principal;
        var email = principal.FindFirst(ClaimTypes.Email)?.Value;
        var emailVerified = principal.FindFirst("email_verified")?.Value;

        // Only trust an email address that Google itself has verified.
        if (string.IsNullOrWhiteSpace(email)
            || !string.Equals(emailVerified, "true", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Google login rejected: no email, or the email is not verified by Google");
            await HttpContext.SignOutAsync(AuthSchemes.External);
            return Redirect($"{frontendUrl}/?error=google_login_failed");
        }

        var firstName = principal.FindFirst(ClaimTypes.GivenName)?.Value
            ?? principal.FindFirst(ClaimTypes.Name)?.Value
            ?? email.Split('@')[0];
        var lastName = principal.FindFirst(ClaimTypes.Surname)?.Value ?? string.Empty;
        var picture = principal.FindFirst("urn:google:picture")?.Value;

        var login = await _authService.GoogleLoginAsync(new GoogleProfile(email, firstName, lastName, picture));

        // The external cookie has done its job.
        await HttpContext.SignOutAsync(AuthSchemes.External);

        // New Google users must add a phone number first; everyone else goes straight in.
        var page = login.IsProfileComplete ? "oauth2/callback" : "complete-profile";
        return Redirect($"{frontendUrl}/{page}?token={Uri.EscapeDataString(login.Token)}");
    }
}
