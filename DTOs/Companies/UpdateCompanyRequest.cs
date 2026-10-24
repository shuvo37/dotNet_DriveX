using System.ComponentModel.DataAnnotations;

namespace DriveX.DTOs.Companies;

public class UpdateCompanyRequest
{
    [Required(ErrorMessage = "Company name is required")]
    public string CompanyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string CompanyEmail { get; set; } = string.Empty;

    // Spring accepted 10 to 15 digits with an optional "+" when updating,
    // so companies saved with an older phone format can still be edited.
    [Required(ErrorMessage = "Phone number is required")]
    [RegularExpression(@"^\+?[0-9]{10,15}$", ErrorMessage = "Invalid phone number")]
    public string CompanyPhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Company address is required")]
    public string CompanyAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "Company city is required")]
    public string CompanyCity { get; set; } = string.Empty;
}
