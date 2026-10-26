using System.ComponentModel.DataAnnotations;

namespace DriveX.DTOs.Bookings;

// The React page also sends "userId". It is ignored on purpose: the booking always belongs
// to the logged-in user (taken from the token).
public class CreateBookingRequest
{
    [Required(ErrorMessage = "paymentId is required")]
    [Range(1, long.MaxValue, ErrorMessage = "paymentId must be positive")]
    public long? PaymentId { get; set; }

    [Required(ErrorMessage = "carId is required")]
    [Range(1, long.MaxValue, ErrorMessage = "carId must be positive")]
    public long? CarId { get; set; }

    [Required(ErrorMessage = "totalprice is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "totalprice must be positive")]
    public double? TotalPrice { get; set; }

    [Required(ErrorMessage = "days is required")]
    [Range(0, int.MaxValue, ErrorMessage = "days cannot be negative")]
    public int? Days { get; set; }

    [Required(ErrorMessage = "hour is required")]
    [Range(0, 23, ErrorMessage = "hour must be between 0 and 23")]
    public int? Hour { get; set; }

    [Required(ErrorMessage = "totalHours is required")]
    [Range(1, int.MaxValue, ErrorMessage = "totalHours must be positive")]
    public int? TotalHours { get; set; }

    [Required(ErrorMessage = "address must not be blank")]
    public string Address { get; set; } = string.Empty;

    // Same format the Payment page already checks: 11 digits starting with 01.
    [Required(ErrorMessage = "phone must not be blank")]
    [RegularExpression(@"^01[0-9]{9}$", ErrorMessage = "Invalid phone number")]
    public string Phone { get; set; } = string.Empty;

    // The city the customer picked on the booking page. It has to match the car's city.
    public string? City { get; set; }
}
