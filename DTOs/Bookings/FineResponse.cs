namespace DriveX.DTOs.Bookings;

// What the staff screen shows for a car being returned. Calculated by the backend, never by the browser.
// Status is "late" or "ontime" (same words the React page already uses).
public record FineResponse(
    long BookingId,
    DateTime RentStart,
    DateTime MustReturnBy,
    int MinutesLate,
    int HoursLate,
    double FineAmount,
    // What is really charged: the fine, but never below the gateway minimum. 0 when there is no fine.
    double AmountToPay,
    string Status,
    DateTime CalculatedAt);
