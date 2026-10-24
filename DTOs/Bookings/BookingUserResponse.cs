namespace DriveX.DTOs.Bookings;

// The only user fields a booking shows. No password hash, no phone, no birth date.
public record BookingUserResponse(long UserId, string FirstName, string LastName, string Email);
