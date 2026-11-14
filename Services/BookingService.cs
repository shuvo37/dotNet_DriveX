using DriveX.DTOs.Bookings;
using DriveX.DTOs.Cars;
using DriveX.Enums;
using DriveX.Exceptions;
using DriveX.Mapping;
using DriveX.Models;
using DriveX.Repositories;
using Microsoft.Extensions.Logging;

namespace DriveX.Services;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookings;
    private readonly ICarRepository _cars;
    private readonly IPaymentRepository _payments;
    private readonly IUserRepository _users;
    private readonly ISubmissionRepository _submissions;
    private readonly IEmailService _emailService;
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        IBookingRepository bookings,
        ICarRepository cars,
        IPaymentRepository payments,
        IUserRepository users,
        ISubmissionRepository submissions,
        IEmailService emailService,
        ILogger<BookingService> logger)
    {
        _bookings = bookings;
        _cars = cars;
        _payments = payments;
        _users = users;
        _submissions = submissions;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<BookingResultResponse> CreateAsync(CreateBookingRequest request, long userId)
    {
        var days = request.Days!.Value;
        var hour = request.Hour!.Value;
        var totalHours = request.TotalHours!.Value;

        if (days * 24 + hour != totalHours)
        {
            throw new AppException("Total hours does not match days and hours");
        }

        var carId = request.CarId!.Value;
        var paymentId = request.PaymentId!.Value;

        var car = await _cars.GetDetailsByIdAsync(carId)
                  ?? throw new AppException($"Car {carId} not found");

        if (!car.IsActive || !car.Company.IsActive)
        {
            throw new AppException("Car is not available");
        }

        CityRule.EnsureCustomerIsInCarCity(request.City, car.Company.CompanyCity);

        var payment = await _payments.GetDetailsByIdAsync(paymentId)
                      ?? throw new AppException($"Payment {paymentId} not found");

        // The payment must be this customer's rental payment for this same car.
        if (payment.UserId != userId || payment.CarId != carId || payment.PaymentType != PaymentType.RENTED_PRICE)
        {
            throw new AppException("Payment does not belong to this booking");
        }

        // A payment that is still PENDING (or failed) has not brought any money, so it can not buy a booking.
        if (payment.PaymentStatus != PaymentStatus.DONE)
        {
            throw new AppException("Payment is not completed");
        }

        // Rental payments only come from the payment gateway, which always gives them a transaction id.
        if (string.IsNullOrEmpty(payment.TransactionId))
        {
            throw new AppException("Payment was not made through the payment gateway");
        }

        if (await _bookings.ExistsByPaymentIdAsync(paymentId))
        {
            throw new AppException("Payment is already used for a booking");
        }

        // Do not trust the price from the browser: it must match the car's hourly rate and what was paid.
        // Example: a car at 12.50 per hour for 26 hours must cost 325.00, not 1.00.
        var expectedPrice = Math.Round(car.PricePerHour * totalHours, 2);

        if (Math.Abs(request.TotalPrice!.Value - expectedPrice) > 0.01)
        {
            throw new AppException("Price does not match the car's hourly rate");
        }

        if (Math.Abs(payment.TotalAmount - expectedPrice) > 0.01)
        {
            throw new AppException("Payment amount does not match the booking price");
        }

        var booking = new Booking
        {
            UserId = userId,
            PaymentId = paymentId,
            CarId = carId,
            TotalPrice = request.TotalPrice!.Value,
            BookingTime = DateTime.Now, // local time, like Java's LocalDateTime.now()
            Days = days,
            Hour = hour,
            TotalHours = totalHours,
            Address = request.Address.Trim(),
            Phone = request.Phone.Trim(),
            BookingStatus = BookingStatus.NOT_DELIVERED
        };

        // Marks the car RENTED and saves the booking together (one transaction inside the repository).
        var result = await _bookings.CreateWithCarRentedAsync(booking);

        if (result == BookingCreateResult.CarNotAvailable)
        {
            throw new AppException("Car is not available");
        }

        if (result == BookingCreateResult.PaymentAlreadyUsed)
        {
            throw new AppException("Payment is already used for a booking");
        }

        // Read it back with its relations so the response shows the real stored state (car RENTED).
        var saved = await _bookings.GetDetailsByIdAsync(booking.BookingId)
                    ?? throw new AppException("Booking not found");

        return new BookingResultResponse("Successfully booked", saved.ToResponse());
    }

    // All bookings of one customer, found by email (the Profile page sends the email from the token).
    public async Task<List<BookingResponse>> GetByEmailAsync(string email)
    {
        var cleanEmail = email.Trim();

        if (!await _users.ExistsByEmailAsync(cleanEmail))
        {
            throw new AppException("User not found");
        }

        var bookings = await _bookings.GetByEmailAsync(cleanEmail);

        return bookings.Select(b => b.ToResponse()).ToList();
    }

    // Cars that have a booking waiting to be handed over (booking status NOT_DELIVERED).
    // The staff "start rental" page lists these in its search box.
    public Task<List<CarResponse>> GetNotDeliveredCarsAsync() =>
        GetCarsWithBookingStatusAsync(BookingStatus.NOT_DELIVERED);

    // Cars that are out with a customer right now (booking status ONGOING).
    // The staff "submit car" page lists these in its search box.
    public Task<List<CarResponse>> GetOngoingCarsAsync() =>
        GetCarsWithBookingStatusAsync(BookingStatus.ONGOING);

    private async Task<List<CarResponse>> GetCarsWithBookingStatusAsync(BookingStatus status)
    {
        var cars = await _cars.GetByBookingStatusAsync(status);

        return cars.Select(c => c.ToResponse()).ToList();
    }

    // The waiting booking of one car, found by the model name the staff member picked.
    public async Task<BookingResponse> GetNotDeliveredBookingAsync(string modelName)
    {
        var cleanName = modelName.Trim();

        var booking = await _bookings.GetFirstByModelNameAndStatusAsync(cleanName, BookingStatus.NOT_DELIVERED)
                      ?? throw new AppException("No undelivered booking found for this car");

        return booking.ToResponse();
    }

    // Staff hand the car over: the rental clock starts now and the customer gets an email.
    public async Task<BookingResponse> StartRentAsync(long bookingId)
    {
        var booking = await _bookings.GetByIdAsync(bookingId)
                      ?? throw new AppException("Booking not found");

        // Without this check, pressing start a second time would reset the start time.
        if (booking.BookingStatus != BookingStatus.NOT_DELIVERED)
        {
            throw new AppException("Rental has already been started");
        }

        booking.RentStart = DateTime.Now; // local time, like Java's LocalDateTime.now()
        booking.Toggle = true;
        booking.BookingStatus = BookingStatus.ONGOING;

        await _bookings.UpdateAsync(booking);

        await TrySendRentStartedEmailAsync(booking);

        return booking.ToResponse();
    }

    // Staff mark the rental time as finished. The booking stays ONGOING until the car is submitted.
    public async Task<BookingResponse> EndRentAsync(long bookingId)
    {
        var booking = await _bookings.GetByIdAsync(bookingId)
                      ?? throw new AppException("Booking not found");

        if (booking.BookingStatus != BookingStatus.ONGOING || !booking.Toggle)
        {
            throw new AppException("Rental is not running");
        }

        booking.RentEnd = DateTime.Now;
        booking.Toggle = false;

        await _bookings.UpdateAsync(booking);

        return booking.ToResponse();
    }

    // The staff screen asks for the fine before returning a car. Same rule is reused when the fine is paid.
    public async Task<FineResponse> GetFineAsync(long bookingId)
    {
        var booking = await _bookings.GetDetailsByIdAsync(bookingId)
                      ?? throw new AppException($"Booking {bookingId} not found");

        // The calculator refuses a booking that is not out on rent.
        return FineCalculator.Calculate(booking, DateTime.Now); // local time, like RentStart
    }

    // The rental is already saved at this point. If the mail server is down, staff should not see
    // an error for a rental that did start, so a failed email is only written to the log.
    private async Task TrySendRentStartedEmailAsync(Booking booking)
    {
        try
        {
            await _emailService.SendEmailAsync(
                booking.User.Email,
                "Your rental has started",
                $"Hi {booking.User.FirstName} {booking.User.LastName},\n\n" +
                $"Your rental for {booking.Car.ModelName} started at {booking.RentStart:dd MMM yyyy, HH:mm}.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not send the rental-started email for booking {BookingId}", booking.BookingId);
        }
    }

    // The running booking of one car. The staff "submit car" page needs it for the return time and the fine.
    public async Task<BookingResponse> GetOngoingBookingOfCarAsync(long carId)
    {
        var booking = await _bookings.GetFirstByCarIdAndStatusAsync(carId, BookingStatus.ONGOING)
                      ?? throw new AppException("No ongoing booking found for this car");

        return booking.ToResponse();
    }

    // Last step of a return: the booking is closed as SUBMITTED. It needs the submission record first.
    public async Task<BookingResponse> MarkSubmittedAsync(long bookingId)
    {
        var booking = await _bookings.GetByIdAsync(bookingId)
                      ?? throw new AppException("Booking not found");

        if (booking.BookingStatus != BookingStatus.ONGOING)
        {
            throw new AppException("Booking is not ongoing");
        }

        if (!await _submissions.ExistsByBookingIdAsync(bookingId))
        {
            throw new AppException("Submit the car before closing the booking");
        }

        booking.BookingStatus = BookingStatus.SUBMITTED;
        await _bookings.UpdateAsync(booking);

        return booking.ToResponse();
    }

    // Admin search page: the running booking of the car with this model name.
    // Model names are unique, so at most one car matches. Nothing found is not an error here:
    // the page shows its "No Bookings Found" screen, so this returns null instead of throwing.
    public async Task<BookingResponse?> SearchOngoingBookingByModelAsync(string modelName)
    {
        var cleanName = modelName.Trim();

        var booking = await _bookings.GetFirstByModelNameAndStatusAsync(cleanName, BookingStatus.ONGOING);

        return booking?.ToResponse();
    }
}
