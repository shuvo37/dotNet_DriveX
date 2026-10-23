using System.ComponentModel.DataAnnotations;

namespace DriveX.DTOs.Auth;

public class RegisterRequest
{
    [Required(ErrorMessage = "First name is required")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    // Bangladeshi mobile format: 11 digits starting with 01 (the Spring service enforced this too).
    [Required(ErrorMessage = "Phone number is required")]
    [RegularExpression(@"^01[0-9]{9}$", ErrorMessage = "Invalid phone number")]
    public string PhoneNumber { get; set; } = string.Empty;

    // Nullable on purpose, so a missing value triggers the "required" message.
    [Required(ErrorMessage = "Date of birth is required")]
    public DateOnly? DateOfBirth { get; set; }

    // BCrypt only uses the first 72 bytes of a password, so longer ones are rejected.
    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    [MaxLength(72, ErrorMessage = "Password must be at most 72 characters")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? ProfileImage { get; set; }
}
