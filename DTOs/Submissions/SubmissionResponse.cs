namespace DriveX.DTOs.Submissions;

// What the API returns for a submission: plain ids, never the Payment, Booking, Car or User entities.
public record SubmissionResponse(
    long SubmissionId,
    long PaymentId,
    long BookingId,
    long CarId,
    long UserId,
    DateTime SubmittedAt);
