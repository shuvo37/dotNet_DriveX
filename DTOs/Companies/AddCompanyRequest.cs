using System.ComponentModel.DataAnnotations;

namespace DriveX.DTOs.Companies;

public class AddCompanyRequest
{
    [Required(ErrorMessage = "Company name is required")]
    public string CompanyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string CompanyEmail { get; set; } = string.Empty;

    // Same rule the Spring service enforced for new companies: 11 digits starting with 01.
    [Required(ErrorMessage = "Phone number is required")]
    [RegularExpression(@"^01[0-9]{9}$", ErrorMessage = "Invalid phone number")]
    public string CompanyPhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Company address is required")]
    public string CompanyAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "Company city is required")]
    public string CompanyCity { get; set; } = string.Empty;
}
