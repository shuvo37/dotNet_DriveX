using System.ComponentModel.DataAnnotations;

namespace DriveX.DTOs.Companies;

public class UpdateCompanyActiveStatusRequest
{
    [Required(ErrorMessage = "Company name is required")]
    public string CompanyName { get; set; } = string.Empty;

    // Nullable on purpose: a missing value is rejected instead of silently becoming false
    // (which would deactivate the company).
    [Required(ErrorMessage = "isActive is required")]
    public bool? IsActive { get; set; }
}
