using DriveX.DTOs.Bookings;
using DriveX.Enums;
using DriveX.Exceptions;
using DriveX.Models;
using DriveX.Options;

namespace DriveX.Services;

// The one place where the late fine is worked out.
//
// Rule: the car must come back TotalHours after the rent started. Only complete late hours count
// (59 minutes late = 0 hours, 2 hours 40 minutes late = 2 hours). Each late hour costs the hourly
// rate plus an extra charge. The extra charge is half the hourly rate, rounded down, and at least 1.
//
// Example: rate 10 -> extra 5, so each late hour costs 15. 2 hours 40 minutes late = 2 x 15 = 30.
// Example: rate 3  -> extra 1 (floor of 1.5), so each late hour costs 4.
//
// The fine is paid through SSLCommerz, which does not accept less than 10. So a fine below 10 is charged
// as 10: AmountToPay = max(10, fine). Example: a fine of 4 is charged as 10, a fine of 30 as 30.
public static class FineCalculator
{
    // booking.Car must be loaded.
    public static FineResponse Calculate(Booking booking, DateTime now)
    {
        if (booking.BookingStatus != BookingStatus.ONGOING || booking.RentStart is null)
        {
            throw new AppException("The rent has not started, or the car was already returned");
        }

        var rentStart = booking.RentStart!.Value;
        var mustReturnBy = rentStart.AddHours(booking.TotalHours);

        var minutesLate = Math.Max(0, (int)Math.Floor((now - mustReturnBy).TotalMinutes));
        var hoursLate = minutesLate / 60;

        var pricePerHour = booking.Car.PricePerHour;
        var extraPerHour = Math.Max(1, Math.Floor(pricePerHour / 2));

        var fineAmount = hoursLate > 0
            ? Math.Round(hoursLate * (pricePerHour + extraPerHour), 2)
            : 0;

        var amountToPay = fineAmount > 0 ? Math.Max(SslCommerzSettings.MinimumAmount, fineAmount) : 0;

        return new FineResponse(
            booking.BookingId,
            rentStart,
            mustReturnBy,
            minutesLate,
            hoursLate,
            fineAmount,
            amountToPay,
            minutesLate > 0 ? "late" : "ontime",
            now);
    }
}
