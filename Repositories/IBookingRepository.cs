using DriveX.Enums;
using DriveX.Models;

namespace DriveX.Repositories;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(long id);
    Task<Booking?> GetDetailsByIdAsync(long id);
    Task<List<Booking>> GetByEmailAsync(string email);
    Task<Booking?> GetFirstByModelNameAndStatusAsync(string modelName, BookingStatus status);
    Task<Booking?> GetFirstByCarIdAndStatusAsync(long carId, BookingStatus status);
    Task<(int Total, List<Booking> Bookings)> SearchAsync(
        DateTime start, DateTime endExclusive, BookingStatus? status, string? carModel, int take);
    Task<List<Booking>> GetOngoingStartedAsync(int take);
    Task<List<Booking>> GetByBookingTimeAsync(DateTime start, DateTime endExclusive, int take);
    Task<bool> ExistsByPaymentIdAsync(long paymentId);
    Task<BookingCreateResult> CreateWithCarRentedAsync(Booking booking);
    Task UpdateAsync(Booking booking);
}
