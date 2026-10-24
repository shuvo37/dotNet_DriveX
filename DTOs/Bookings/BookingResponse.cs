using DriveX.DTOs.Cars;
using DriveX.DTOs.Payments;
using System.Text.Json.Serialization;
using DriveX.Enums;

namespace DriveX.DTOs.Bookings;

// What the API returns for a booking. The staff and profile pages added in later steps reuse it.
public record BookingResponse(
    long BookingId,
    BookingUserResponse User,
    PaymentResponse Payment,
    CarResponse Car,
    // Spring named this field "totalprice" and Profile.jsx, SearchBookingCar.jsx and
    // CarRentTimeStart.jsx read booking.totalprice, so the JSON name stays lowercase.
    [property: JsonPropertyName("totalprice")] double TotalPrice,
    DateTime BookingTime,
    int Days,
    int Hour,
    int TotalHours,
    string Address,
    string Phone,
    BookingStatus? BookingStatus,
    DateTime? RentStart,
    DateTime? RentEnd,
    bool Toggle);
