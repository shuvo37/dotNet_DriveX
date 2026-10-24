using System.ComponentModel.DataAnnotations;
using DriveX.Enums;

namespace DriveX.DTOs.Cars;

// The car is identified by the id in the URL. The React page also sends companyName and
// modelName, but they never changed anything in Spring, so they are simply ignored here.
public class UpdateCarRequest
{
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
