using System.ComponentModel.DataAnnotations;
using DriveX.Enums;

namespace DriveX.DTOs.Payments;

// Staff pay a customer's late fine. There is no amount here: the server works it out from the booking.
public class StartFinePaymentRequest
{
    [Required(ErrorMessage = "bookingId is required")]
    [Range(1, long.MaxValue, ErrorMessage = "bookingId must be positive")]
    public long? BookingId { get; set; }

    // How the customer handed the money over, as the staff screen already asks (bKash or Nagad).
    [Required(ErrorMessage = "paymentMethod is required")]
    public PaymentMethod? PaymentMethod { get; set; }
}
