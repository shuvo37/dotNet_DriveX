namespace DriveX.DTOs.Bookings;

// { "message": "...", "bookings": { ... } }. The key is "bookings" because that is what Payment.jsx reads.
public record BookingResultResponse(string Message, BookingResponse Bookings);
