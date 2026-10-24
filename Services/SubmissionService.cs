using DriveX.Data;
using DriveX.DTOs.Submissions;
using DriveX.Enums;
using DriveX.Exceptions;
using DriveX.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DriveX.Services;

public class SubmissionService : ISubmissionService
{
    private readonly DriveXDbContext _db;
    private readonly IEmailService _emailService;
    private readonly ILogger<SubmissionService> _logger;

    public SubmissionService(DriveXDbContext db, IEmailService emailService, ILogger<SubmissionService> logger)
    {
        _db = db;
        _emailService = emailService;
        _logger = logger;
    }

    // Staff record that the customer returned the car. The ids sent by the page must all belong to
    // one real, running booking, so a wrong id can not attach a submission to someone else's booking.
    public async Task<SubmissionResponse> CreateAsync(SubmitCarRequest request)
    {
        var bookingId = request.BookingId!.Value;

        var booking = await _db.Bookings
                          .AsNoTracking()
                          .Include(b => b.User)
                          .Include(b => b.Car)
                          .FirstOrDefaultAsync(b => b.BookingId == bookingId)
                      ?? throw new AppException("Booking not found");

        if (booking.BookingStatus != BookingStatus.ONGOING)
        {
            throw new AppException("Booking is not ongoing");
        }

        if (booking.PaymentId != request.PaymentId!.Value ||
            booking.CarId != request.CarId!.Value ||
            booking.UserId != request.UserId!.Value)
        {
            throw new AppException("Submission details do not match the booking");
        }

        if (await _db.Submissions.AnyAsync(s => s.BookingId == bookingId))
        {
            throw new AppException("Car already submitted for this booking");
        }

        var submission = new Submission
        {
            PaymentId = booking.PaymentId,
            BookingId = bookingId,
            CarId = booking.CarId,
            UserId = booking.UserId,
            SubmittedAt = DateTime.Now // server time, like Java's LocalDateTime.now()
        };

        _db.Submissions.Add(submission);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // booking_id and payment_id are unique in the submission table; this covers a double click.
            throw new AppException("Car already submitted for this booking");
        }

        await TrySendSubmissionEmailAsync(booking, submission);

        return new SubmissionResponse(
            submission.SubmissionId,
            submission.PaymentId,
            submission.BookingId,
            submission.CarId,
            submission.UserId,
            submission.SubmittedAt);
    }

    // The submission is already saved at this point. If the mail server is down, staff should not
    // see an error for a car that was submitted, so a failed email is only written to the log.
    private async Task TrySendSubmissionEmailAsync(Booking booking, Submission submission)
    {
        try
        {
            await _emailService.SendEmailAsync(
                booking.User.Email,
                "Car submission confirmed",
                $"Hi {booking.User.FirstName} {booking.User.LastName},\n\n" +
                $"We've received your car ({booking.Car.ModelName}) at {submission.SubmittedAt:dd MMM yyyy, HH:mm}. Thanks!");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not send the submission email for booking {BookingId}", booking.BookingId);
        }
    }
}
