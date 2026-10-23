using System.ComponentModel.DataAnnotations;

namespace DriveX.DTOs.PasswordReset;

public class ResetPasswordRequest
{
    [Required(ErrorMessage = "Email is required")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Code is required")]
    [RegularExpression(@"^[0-9]{4}$", ErrorMessage = "Code must be 4 digits")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required")]
    [MinLength(8, ErrorMessage = "Password too short")]
    [MaxLength(72, ErrorMessage = "Password must be at most 72 characters")]
    public string NewPassword { get; set; } = string.Empty;
}
