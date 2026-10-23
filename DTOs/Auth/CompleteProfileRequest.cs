using System.ComponentModel.DataAnnotations;

namespace DriveX.DTOs.Auth;

// Used by people who signed up with Google and still need to add a phone number.
public class CompleteProfileRequest
{
    [Required(ErrorMessage = "Phone number is required")]
    [RegularExpression(@"^\+?[0-9]{10,15}$", ErrorMessage = "Invalid phone number")]
    public string PhoneNumber { get; set; } = string.Empty;
}
