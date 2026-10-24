using DriveX.DTOs.Bookings;
using DriveX.DTOs.Cars;

namespace DriveX.Services;

public interface IBookingService
{
    Task<BookingResultResponse> CreateAsync(CreateBookingRequest request, long userId);
    Task<List<BookingResponse>> GetByEmailAsync(string email);
    Task<List<CarResponse>> GetNotDeliveredCarsAsync();
    Task<BookingResponse> GetNotDeliveredBookingAsync(string modelName);
    Task<BookingResponse> StartRentAsync(long bookingId);
    Task<BookingResponse> EndRentAsync(long bookingId);
    Task<List<CarResponse>> GetOngoingCarsAsync();
    Task<BookingResponse> GetOngoingBookingOfCarAsync(long carId);
    Task<BookingResponse> MarkSubmittedAsync(long bookingId);
}
