using System.ComponentModel.DataAnnotations;
using DriveX.Enums;

namespace DriveX.DTOs.Payments;

// What the Payment page sends to start a real payment.
// There is no amount and no status here on purpose: the server works out the price from the car's
// hourly rate, and only the payment gateway can say that money arrived.
public class StartPaymentRequest
{
    [Required(ErrorMessage = "carId is required")]
    [Range(1, long.MaxValue, ErrorMessage = "carId must be positive")]
    public long? CarId { get; set; }

    [Required(ErrorMessage = "days is required")]
    [Range(0, int.MaxValue, ErrorMessage = "days cannot be negative")]
    public int? Days { get; set; }

    [Required(ErrorMessage = "hour is required")]
    [Range(0, 23, ErrorMessage = "hour must be between 0 and 23")]
    public int? Hour { get; set; }

    [Required(ErrorMessage = "paymentMethod is required")]
    public PaymentMethod? PaymentMethod { get; set; }

    // The city the customer picked on the booking page. It has to match the car's city.
    public string? City { get; set; }

    [Required(ErrorMessage = "address must not be blank")]
    public string Address { get; set; } = string.Empty;

    // Same format the booking uses: 11 digits starting with 01. SSLCommerz needs a phone number.
    [Required(ErrorMessage = "phone must not be blank")]
    [RegularExpression(@"^01[0-9]{9}$", ErrorMessage = "Invalid phone number")]
    public string Phone { get; set; } = string.Empty;
}
