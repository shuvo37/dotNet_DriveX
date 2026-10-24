using System.ComponentModel.DataAnnotations;
using DriveX.Enums;

namespace DriveX.DTOs.Cars;

public class AddCarRequest
{
    // The company is picked by name, as in the Spring API.
    [Required(ErrorMessage = "Company name is required")]
    public string CompanyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Model name can't be blank")]
    public string ModelName { get; set; } = string.Empty;

    // Nullable on purpose, so a missing value triggers the "required" message.
    [Required(ErrorMessage = "pricePerHour is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be positive")]
    public double? PricePerHour { get; set; }

    [Required(ErrorMessage = "rentalStatus can't be blank")]
    public RentalStatus? RentalStatus { get; set; }

    [Required(ErrorMessage = "Seats is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Seats must be at least one")]
    public int? Seats { get; set; }

    [Required(ErrorMessage = "fuel can't be blank")]
    public FuelType? Fuel { get; set; }

    [Required(ErrorMessage = "Image URL can't be blank")]
    public string ImageUrl { get; set; } = string.Empty;

    public string? Tag { get; set; }
}
