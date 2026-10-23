namespace DriveX.DTOs.Auth;

// Exactly what the frontend gets after login. There is no password field here, by design.
public record LoginResponse(
    long UserId,
    string FirstName,
    string LastName,
    string Email,
    string? ProfileImage,
    string Role,
    string Token);
