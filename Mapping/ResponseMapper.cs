using DriveX.DTOs.Bookings;
using DriveX.DTOs.Cars;
using DriveX.DTOs.Companies;
using DriveX.DTOs.Payments;
using DriveX.Models;

namespace DriveX.Mapping;

// Turns database entities into response DTOs. Each method needs the related data it reads
// to be loaded already (Include), for example Car needs its Company.
public static class ResponseMapper
{
    public static CompanyResponse ToResponse(this Company c) => new(
        c.CompanyId,
        c.CompanyName,
        c.CompanyEmail,
        c.CompanyPhoneNumber,
        c.CompanyCity,
        c.CompanyAddress,
        c.IsActive);

    public static CarResponse ToResponse(this Car c) => new(
        c.CarId,
        c.Company.ToResponse(),
        c.ModelName,
        c.PricePerHour,
        c.RentalStatus,
        c.Seats,
        c.Fuel,
        c.ImageUrl,
        c.Tag,
        c.IsActive);

    public static PaymentResponse ToResponse(this Payment p) => new(
        p.PaymentId,
        p.CarId,
        p.UserId,
        p.PaymentMethod,
        p.PaymentStatus,
        p.PaymentType,
        p.TotalAmount,
        p.PaymentTime);

    public static BookingResponse ToResponse(this Booking b) => new(
        b.BookingId,
        new BookingUserResponse(b.User.UserId, b.User.FirstName, b.User.LastName, b.User.Email),
        b.Payment.ToResponse(),
        b.Car.ToResponse(),
        b.TotalPrice,
        b.BookingTime,
        b.Days,
        b.Hour,
        b.TotalHours,
        b.Address,
        b.Phone,
        b.BookingStatus,
        b.RentStart,
        b.RentEnd,
        b.Toggle);
}
