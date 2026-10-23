using System.ComponentModel.DataAnnotations;

namespace DriveX.DTOs.PasswordReset;

public class VerifyCodeRequest
{
    [Required(ErrorMessage = "Email is required")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Code is required")]
    [RegularExpression(@"^[0-9]{4}$", ErrorMessage = "Code must be 4 digits")]
    public string Code { get; set; } = string.Empty;
}
