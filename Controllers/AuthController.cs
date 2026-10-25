using DriveX.DTOs.Auth;
using DriveX.Exceptions;
using DriveX.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DriveX.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register(RegisterRequest request)
    {
        var response = await _authService.RegisterAsync(request);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        return Ok(await _authService.LoginAsync(request));
    }

    // Needs a valid token. The email comes from the token itself ("sub" claim),
    // so a user can only complete their own profile.
    [Authorize]
    [HttpPost("complete-profile")]
    public async Task<ActionResult<LoginResponse>> CompleteProfile(CompleteProfileRequest request)
    {
        var email = User.FindFirst("sub")?.Value
            ?? throw new AppException("Invalid token");

        return Ok(await _authService.CompleteProfileAsync(request, email));
    }

    // Same URL and query as Spring. Returns a UserResponse, never the entity with the password hash.
    [HttpGet("get-user-by-email")]
    public async Task<ActionResult<UserResponse>> GetUserByEmail([FromQuery] string email)
    {
        return Ok(await _authService.GetByEmailAsync(email));
    }

    [HttpPatch("role-update")]
    public async Task<ActionResult<UserResponse>> UpdateRole([FromQuery] string email, [FromQuery] string role)
    {
        return Ok(await _authService.UpdateRoleAsync(email, role));
    }
}
