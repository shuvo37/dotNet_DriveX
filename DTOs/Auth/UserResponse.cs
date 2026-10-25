using DriveX.Enums;

namespace DriveX.DTOs.Auth;

// A user as the frontend may see them. There is no Password field here, by design.
public record UserResponse(
    long UserId,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    string? ProfileImage,
    DateTime? CreatedAt,
    Role? Role,
    bool IsProfileComplete);
