using DriveX.Data;
using DriveX.DTOs.Payments;
using DriveX.Enums;
using DriveX.Exceptions;
using DriveX.Mapping;
using DriveX.Models;
using DriveX.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DriveX.Services;

public class PaymentService : IPaymentService
{
    private readonly DriveXDbContext _db;
    private readonly ISslCommerzClient _sslCommerz;
    private readonly SslCommerzSettings _sslCommerzSettings;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        DriveXDbContext db,
        ISslCommerzClient sslCommerz,
        IOptions<SslCommerzSettings> sslCommerzSettings,
        ILogger<PaymentService> logger)
    {
        _db = db;
        _sslCommerz = sslCommerz;
        _sslCommerzSettings = sslCommerzSettings.Value;
        _logger = logger;
    }

    // Step one of a real payment. The customer is the logged-in user, the price is worked out here,
    // and the payment starts as PENDING. Only the gateway can turn it into DONE (a later step).
    public async Task<StartPaymentResponse> StartGatewayPaymentAsync(StartPaymentRequest request, long userId)
    {
        var totalHours = request.Days!.Value * 24 + request.Hour!.Value;

        if (totalHours < 1)
        {
            throw new AppException("Minimum rental is 1 hour");
        }

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId)
                   ?? throw new AppException($"User {userId} not found");

        var carId = request.CarId!.Value;

        var car = await _db.Cars.AsNoTracking().Include(c => c.Company)
                      .FirstOrDefaultAsync(c => c.CarId == carId)
                  ?? throw new AppException($"Car {carId} not found");

        CityRule.EnsureCustomerIsInCarCity(request.City, car.Company.CompanyCity);

        if (!car.IsActive || !car.Company.IsActive || car.RentalStatus != RentalStatus.AVAILABLE)
        {
            throw new AppException("Car is not available");
        }

        // Example: a car at 12.50 per hour for 26 hours costs 325.00, whatever the browser says.
        var amount = Math.Round(car.PricePerHour * totalHours, 2);

        if (amount < SslCommerzSettings.MinimumAmount)
        {
            throw new AppException($"The minimum payment is {SslCommerzSettings.MinimumAmount:0.00}");
        }

        // Our own id for this payment, e.g. "DRX-7-12-A1B2C3D4" (user 7, car 12). The gateway sends it back.
        var transactionId = $"DRX-{userId}-{carId}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

        var payment = new Payment
        {
            CarId = carId,
            UserId = userId,
            PaymentMethod = request.PaymentMethod!.Value,
            PaymentStatus = PaymentStatus.PENDING,
            PaymentType = PaymentType.RENTED_PRICE,
            TotalAmount = amount,
            PaymentTime = DateTime.Now,
            TransactionId = transactionId
        };

        return await SavePendingAndStartAsync(payment, new SslCommerzInitRequest(
            TranId: transactionId,
            Amount: (decimal)amount,
            CustomerName: $"{user.FirstName} {user.LastName}".Trim(),
            CustomerEmail: user.Email,
            CustomerPhone: request.Phone.Trim(),
            CustomerAddress: request.Address.Trim(),
            CustomerCity: string.IsNullOrWhiteSpace(request.City) ? car.Company.CompanyCity : request.City.Trim(),
            ProductName: $"{car.ModelName} rental, {totalHours}h"));
    }

    // Staff pay a customer's late fine on SSLCommerz. The customer handed the money over at the office,
    // so the staff pay it digitally and it leaves a trace. The amount comes from FineCalculator, never from
    // the browser. A fine below the gateway minimum is charged as the minimum: max(10, fine).
    public async Task<StartPaymentResponse> StartFineGatewayPaymentAsync(StartFinePaymentRequest request, bool callerIsStaff)
    {
        if (!callerIsStaff)
        {
            throw new AppException("Only staff can pay a fine");
        }

        var bookingId = request.BookingId!.Value;

        var booking = await _db.Bookings.AsNoTracking()
                          .Include(b => b.Car).ThenInclude(c => c.Company)
                          .Include(b => b.User)
                          .FirstOrDefaultAsync(b => b.BookingId == bookingId)
                      ?? throw new AppException($"Booking {bookingId} not found");

        var fine = FineCalculator.Calculate(booking, DateTime.Now);

        if (fine.AmountToPay <= 0)
        {
            throw new AppException("There is no fine to pay for this booking");
        }

        // e.g. "FIN-83-A1B2C3D4" (booking 83). The gateway sends it back.
        var transactionId = $"FIN-{bookingId}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

        // The fine belongs to the customer who rented the car, not to the staff member who pays it.
        var payment = new Payment
        {
            CarId = booking.CarId,
            UserId = booking.UserId,
            PaymentMethod = request.PaymentMethod!.Value,
            PaymentStatus = PaymentStatus.PENDING,
            PaymentType = PaymentType.FINE,
            TotalAmount = fine.AmountToPay,
            PaymentTime = DateTime.Now,
            TransactionId = transactionId
        };

        return await SavePendingAndStartAsync(payment, new SslCommerzInitRequest(
            TranId: transactionId,
            Amount: (decimal)fine.AmountToPay,
            CustomerName: $"{booking.User.FirstName} {booking.User.LastName}".Trim(),
            CustomerEmail: booking.User.Email,
            CustomerPhone: booking.Phone,
            CustomerAddress: booking.Address,
            CustomerCity: booking.Car.Company.CompanyCity,
            ProductName: $"Late fine, {booking.Car.ModelName}, {fine.HoursLate}h late",
            CallbackPath: "/api/payment/sslcommerz/fine"));
    }

    // Saves the payment as PENDING, then asks SSLCommerz for the payment page.
    private async Task<StartPaymentResponse> SavePendingAndStartAsync(Payment payment, SslCommerzInitRequest init)
    {
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        try
        {
            var session = await _sslCommerz.InitiateAsync(init);
            return new StartPaymentResponse(payment.PaymentId, session.GatewayPageUrl);
        }
        catch
        {
            // The gateway never opened a payment page, so this payment can not be completed.
            payment.PaymentStatus = PaymentStatus.FAILED;
            await _db.SaveChangesAsync();
            throw;
        }
    }

    // The customer's browser says "I paid", but a browser can say anything. So we ask SSLCommerz directly:
    // is this val_id valid, is it for this payment, and is the amount what we asked for?
    // Example: a 325.00 payment is only DONE if SSLCommerz confirms 325.00 BDT for the same transaction id.
    // If anything does not match, the payment stays PENDING and nothing is unlocked.
    public async Task<GatewayPaymentOutcome> ConfirmGatewayPaymentAsync(string? transactionId, string? valId)
    {
        if (string.IsNullOrWhiteSpace(transactionId) || string.IsNullOrWhiteSpace(valId))
        {
            return new GatewayPaymentOutcome(false, null);
        }

        var payment = await _db.Payments.FirstOrDefaultAsync(p => p.TransactionId == transactionId);

        if (payment is null)
        {
            _logger.LogWarning("SSLCommerz callback for unknown transaction {TranId}", transactionId);
            return new GatewayPaymentOutcome(false, null);
        }

        // The page was refreshed or the gateway called twice. Already confirmed, so nothing to do.
        if (payment.PaymentStatus == PaymentStatus.DONE)
        {
            return new GatewayPaymentOutcome(true, payment.PaymentId);
        }

        var validation = await _sslCommerz.ValidateAsync(valId);

        var amountMatches = Math.Abs((double)validation.Amount - payment.TotalAmount) <= 0.01;

        if (!validation.IsValid ||
            validation.TranId != payment.TransactionId ||
            validation.Currency != _sslCommerzSettings.Currency ||
            !amountMatches)
        {
            _logger.LogWarning(
                "Payment {TranId} not confirmed. Gateway status {Status}, amount {Amount} {Currency}, expected {Expected}",
                transactionId, validation.Status, validation.Amount, validation.Currency, payment.TotalAmount);
            return new GatewayPaymentOutcome(false, null);
        }

        payment.PaymentStatus = PaymentStatus.DONE;
        payment.PaymentTime = DateTime.Now;
        await _db.SaveChangesAsync();

        return new GatewayPaymentOutcome(true, payment.PaymentId);
    }
}
