using DriveX.Data;
using DriveX.Enums;
using DriveX.Models;
using Microsoft.EntityFrameworkCore;

namespace DriveX.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly DriveXDbContext _context;

    public BookingRepository(DriveXDbContext context)
    {
        _context = context;
    }

    // Tracked, with user, payment, car and company loaded (the service changes it, then calls UpdateAsync)
    public async Task<Booking?> GetByIdAsync(long id)
    {
        return await WithRelations().FirstOrDefaultAsync(b => b.BookingId == id);
    }

    // Read-only, with user, payment, car and company loaded
    public async Task<Booking?> GetDetailsByIdAsync(long id)
    {
        return await WithRelations()
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.BookingId == id);
    }

    // Read-only, all bookings of one customer, oldest first
    public async Task<List<Booking>> GetByEmailAsync(string email)
    {
        return await WithRelations()
            .AsNoTracking()
            .Where(b => b.User.Email == email)
            .OrderBy(b => b.BookingId)
            .ToListAsync();
    }

    // Read-only, the oldest booking of the car with this model name that has this status
    public async Task<Booking?> GetFirstByModelNameAndStatusAsync(string modelName, BookingStatus status)
    {
        return await WithRelations()
            .AsNoTracking()
            .Where(b => b.Car.ModelName == modelName && b.BookingStatus == status)
            .OrderBy(b => b.BookingId)
            .FirstOrDefaultAsync();
    }

    // Read-only, the oldest booking of this car that has this status
    public async Task<Booking?> GetFirstByCarIdAndStatusAsync(long carId, BookingStatus status)
    {
        return await WithRelations()
            .AsNoTracking()
            .Where(b => b.CarId == carId && b.BookingStatus == status)
            .OrderBy(b => b.BookingId)
            .FirstOrDefaultAsync();
    }

    // Read-only, for the admin analyst: bookings made in [start, endExclusive), newest first.
    // Returns how many match in total and the newest 'take' of them (with the car, without the customer).
    public async Task<(int Total, List<Booking> Bookings)> SearchAsync(
        DateTime start, DateTime endExclusive, BookingStatus? status, string? carModel, int take)
    {
        var query = _context.Bookings
            .AsNoTracking()
            .Where(b => b.BookingTime >= start && b.BookingTime < endExclusive);

        if (status != null)
        {
            query = query.Where(b => b.BookingStatus == status);
        }

        if (!string.IsNullOrWhiteSpace(carModel))
        {
            query = query.Where(b => b.Car.ModelName.Contains(carModel));
        }

        var total = await query.CountAsync();

        var bookings = await query
            .Include(b => b.Car)
            .OrderByDescending(b => b.BookingTime)
            .Take(take)
            .ToListAsync();

        return (total, bookings);
    }

    // Read-only, ONGOING bookings whose rent has started, with the car
    public async Task<List<Booking>> GetOngoingStartedAsync(int take)
    {
        return await _context.Bookings
            .AsNoTracking()
            .Include(b => b.Car)
            .Where(b => b.BookingStatus == BookingStatus.ONGOING && b.RentStart != null)
            .OrderBy(b => b.BookingId)
            .Take(take)
            .ToListAsync();
    }

    // Read-only, bookings made in [start, endExclusive), with the car
    public async Task<List<Booking>> GetByBookingTimeAsync(DateTime start, DateTime endExclusive, int take)
    {
        return await _context.Bookings
            .AsNoTracking()
            .Include(b => b.Car)
            .Where(b => b.BookingTime >= start && b.BookingTime < endExclusive)
            .OrderBy(b => b.BookingId)
            .Take(take)
            .ToListAsync();
    }

    public async Task<bool> ExistsByPaymentIdAsync(long paymentId)
    {
        return await _context.Bookings.AnyAsync(b => b.PaymentId == paymentId);
    }

    // Marks the car RENTED and saves the booking as ONE unit: both happen or neither.
    // The car is only changed if it is still AVAILABLE, in one atomic UPDATE. If two customers book
    // the same car at the same moment, only one UPDATE changes a row, so the other gets CarNotAvailable.
    public async Task<BookingCreateResult> CreateWithCarRentedAsync(Booking booking)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        var carsUpdated = await _context.Cars
            .Where(c => c.CarId == booking.CarId && c.RentalStatus == RentalStatus.AVAILABLE)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.RentalStatus, RentalStatus.RENTED));

        if (carsUpdated == 0)
        {
            return BookingCreateResult.CarNotAvailable; // leaving without Commit rolls everything back
        }

        _context.Bookings.Add(booking);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // payment_id is unique in the bookings table; this covers a double click.
            _context.Entry(booking).State = EntityState.Detached;
            return BookingCreateResult.PaymentAlreadyUsed;
        }

        await transaction.CommitAsync();
        return BookingCreateResult.Created;
    }

    // The booking must come from this repository (tracked)
    public async Task UpdateAsync(Booking booking)
    {
        await _context.SaveChangesAsync();
    }

    private IQueryable<Booking> WithRelations() =>
        _context.Bookings
            .Include(b => b.User)
            .Include(b => b.Payment)
            .Include(b => b.Car).ThenInclude(c => c.Company);
}
