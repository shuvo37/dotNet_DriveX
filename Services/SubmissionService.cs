using DriveX.DTOs.Submissions;
using DriveX.Enums;
using DriveX.Exceptions;
using DriveX.Models;
using DriveX.Repositories;
using Microsoft.Extensions.Logging;

namespace DriveX.Services;

public class SubmissionService : ISubmissionService
{
    private readonly IBookingRepository _bookings;
    private readonly ISubmissionRepository _submissions;
    private readonly IEmailService _emailService;
    private readonly ILogger<SubmissionService> _logger;

    public SubmissionService(
        IBookingRepository bookings,
        ISubmissionRepository submissions,
        IEmailService emailService,
        ILogger<SubmissionService> logger)
    {
        _bookings = bookings;
        _submissions = submissions;
        _emailService = emailService;
        _logger = logger;
    }

    // Staff record that the customer returned the car. The ids sent by the page must all belong to
    // one real, running booking, so a wrong id can not attach a submission to someone else's booking.
    public async Task<SubmissionResponse> CreateAsync(SubmitCarRequest request)
    {
        var bookingId = request.BookingId!.Value;

        var booking = await _bookings.GetDetailsByIdAsync(bookingId)
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

        if (await _submissions.ExistsByBookingIdAsync(bookingId))
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

        // The unique indexes on booking_id and payment_id also protect against a double click.
        if (!await _submissions.AddAsync(submission))
        {
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
