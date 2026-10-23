using DriveX.Data;
using DriveX.DTOs.Auth;
using DriveX.Enums;
using DriveX.Exceptions;
using DriveX.Models;
using Microsoft.EntityFrameworkCore;

namespace DriveX.Services;

public class AuthService : IAuthService
{
    // The register form tells users "Must be 16+ to rent", so the server enforces the same rule.
    private const int MinimumAge = 16;

    // Same strength as Spring's default BCryptPasswordEncoder.
    private const int BcryptWorkFactor = 10;

    private readonly DriveXDbContext _db;
    private readonly IJwtService _jwtService;

    public AuthService(DriveXDbContext db, IJwtService jwtService)
    {
        _db = db;
        _jwtService = jwtService;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        // Google-only accounts have an empty password, so they can never pass this check.
        // The same message for every failure avoids revealing which emails exist.
        if (user is null
            || string.IsNullOrEmpty(user.Password)
            || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
        {
            throw new AppException("Invalid email or password");
        }

        return BuildLoginResponse(user);
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        if (request.Password != request.ConfirmPassword)
        {
            throw new AppException("Passwords don't match");
        }

        var dateOfBirth = request.DateOfBirth!.Value; // the [Required] attribute guarantees a value
        var today = DateOnly.FromDateTime(DateTime.Today);

        if (dateOfBirth >= today)
        {
            throw new AppException("Date of birth must be in the past");
        }

        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth > today.AddYears(-age))
        {
            age--; // birthday has not happened yet this year
        }

        if (age < MinimumAge)
        {
            throw new AppException($"You must be at least {MinimumAge} years old");
        }

        // Spring deleted an existing account with an empty password and let the new
        // registration take its place. Anyone could use that to hijack a Google
        // account by registering its email, so here an existing email is always refused.
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
        {
            throw new AppException("Email already exists");
        }

        var user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password, BcryptWorkFactor),
            PhoneNumber = request.PhoneNumber,
            DateOfBirth = dateOfBirth,
            ProfileImage = request.ProfileImage,
            CreatedAt = DateTime.Now, // local time, like LocalDateTime.now() in Java
            Role = Role.USER,
            IsProfileComplete = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return new RegisterResponse("Account created successfully", user.UserId);
    }

    public async Task<LoginResponse> CompleteProfileAsync(CompleteProfileRequest request, string email)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email)
            ?? throw new AppException("User not found");

        user.PhoneNumber = request.PhoneNumber;
        user.IsProfileComplete = true;
        await _db.SaveChangesAsync();

        return BuildLoginResponse(user);
    }

    public async Task<GoogleLoginResult> GoogleLoginAsync(GoogleProfile profile)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == profile.Email);

        if (user is null)
        {
            // First Google sign-in: save only what Google gives us. The phone number
            // is collected later on the complete-profile page.
            user = new User
            {
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                Email = profile.Email,
                Password = string.Empty, // no password: this account signs in with Google
                ProfileImage = profile.Picture,
                CreatedAt = DateTime.Now,
                Role = Role.USER,
                IsProfileComplete = false
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        return new GoogleLoginResult(_jwtService.GenerateToken(user), user.IsProfileComplete);
    }

    // Shared by login and complete-profile, so both return exactly the same shape.
    private LoginResponse BuildLoginResponse(User user)
    {
        return new LoginResponse(
            user.UserId,
            user.FirstName,
            user.LastName,
            user.Email,
            user.ProfileImage,
            (user.Role ?? Role.USER).ToString(),
            _jwtService.GenerateToken(user));
    }
}
