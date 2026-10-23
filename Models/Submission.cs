namespace DriveX.Models;

// Maps to the existing "submission" table: a car being returned for a booking.
public class Submission
{
    public long SubmissionId { get; set; }

    public long PaymentId { get; set; }
    public Payment Payment { get; set; } = null!;

    public long BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    public long CarId { get; set; }
    public Car Car { get; set; } = null!;

    public long UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime SubmittedAt { get; set; }
}
