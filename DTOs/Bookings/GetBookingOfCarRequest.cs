using System.ComponentModel.DataAnnotations;

namespace DriveX.DTOs.Bookings;

// The React page also sends "email" (the staff member's own email). Spring never used it
// to find the booking, so it is ignored here too.
public class GetBookingOfCarRequest
{
    [Required(ErrorMessage = "carId cannot be null")]
    [Range(1, long.MaxValue, ErrorMessage = "carId must be positive")]
    public long? CarId { get; set; }
}
