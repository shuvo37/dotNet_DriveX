using System.ComponentModel.DataAnnotations;

namespace DriveX.DTOs.Submissions;

// The React page also sends "submittedAt". Spring ignored it and used the server time, so do we.
public class SubmitCarRequest
{
    [Required(ErrorMessage = "paymentId cannot be null")]
    [Range(1, long.MaxValue, ErrorMessage = "paymentId must be positive")]
    public long? PaymentId { get; set; }

    [Required(ErrorMessage = "bookingId cannot be null")]
    [Range(1, long.MaxValue, ErrorMessage = "bookingId must be positive")]
    public long? BookingId { get; set; }

    [Required(ErrorMessage = "carId cannot be null")]
    [Range(1, long.MaxValue, ErrorMessage = "carId must be positive")]
    public long? CarId { get; set; }

    // The customer who rented the car (not the staff member).
    [Required(ErrorMessage = "userId cannot be null")]
    [Range(1, long.MaxValue, ErrorMessage = "userId must be positive")]
    public long? UserId { get; set; }
}
