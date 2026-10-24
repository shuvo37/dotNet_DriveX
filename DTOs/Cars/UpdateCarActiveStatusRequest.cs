using System.ComponentModel.DataAnnotations;

namespace DriveX.DTOs.Cars;

public class UpdateCarActiveStatusRequest
{
    [Required(ErrorMessage = "Model name is required")]
    public string ModelName { get; set; } = string.Empty;

    // Nullable on purpose: a missing value is rejected instead of silently becoming false.
    [Required(ErrorMessage = "isActive is required")]
    public bool? IsActive { get; set; }
}
