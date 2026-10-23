using System.ComponentModel.DataAnnotations;

namespace DriveX.DTOs.PasswordReset;

// Body of check-email and resend-code.
public class EmailRequest
{
    [Required(ErrorMessage = "Email is required")]
    public string Email { get; set; } = string.Empty;
}
