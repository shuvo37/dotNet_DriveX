using DriveX.Enums;

namespace DriveX.Models;

// Maps to the existing "users" table. Never return this class from an endpoint:
// it holds the password hash. Responses will use DTOs instead.
public class User
{
    public long UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty; // BCrypt hash
    public string? PhoneNumber { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? ProfileImage { get; set; }
    public DateTime? CreatedAt { get; set; }
    public Role? Role { get; set; }
    public bool IsProfileComplete { get; set; }
}
