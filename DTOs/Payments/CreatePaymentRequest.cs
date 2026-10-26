using System.ComponentModel.DataAnnotations;
using DriveX.Enums;

namespace DriveX.DTOs.Payments;

public class CreatePaymentRequest
{
    [Required(ErrorMessage = "carId is required")]
    [Range(1, long.MaxValue, ErrorMessage = "carId must be positive")]
    public long? CarId { get; set; }

    // Only used when staff record a fine for a customer. For a customer's own payment the
    // server takes the user from the login token and ignores this value.
    public long? UserId { get; set; }

    [Required(ErrorMessage = "paymentMethod is required")]
    public PaymentMethod? PaymentMethod { get; set; }

    [Required(ErrorMessage = "paymentStatus is required")]
    public PaymentStatus? PaymentStatus { get; set; }

    [Required(ErrorMessage = "paymentType is required")]
    public PaymentType? PaymentType { get; set; }

    [Required(ErrorMessage = "totalAmount is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "totalAmount must be positive")]
    public double? TotalAmount { get; set; }

    // The city the customer picked on the booking page. Checked against the car's city for
    // rental payments (RENTED_PRICE). Staff fine payments do not need it.
    public string? City { get; set; }
}
