using DriveX.Data;
using DriveX.Enums;
using DriveX.Models;
using Microsoft.EntityFrameworkCore;

namespace DriveX.Repositories;

public class CarRepository : ICarRepository
{
    private readonly DriveXDbContext _context;

    public CarRepository(DriveXDbContext context)
    {
        _context = context;
    }

    // Tracked, with the company loaded (the response needs it)
    public async Task<Car?> GetByIdAsync(long id)
    {
        return await _context.Cars
            .Include(c => c.Company)
            .FirstOrDefaultAsync(c => c.CarId == id);
    }

    // Read-only, with the company loaded
    public async Task<Car?> GetDetailsByIdAsync(long id)
    {
        return await _context.Cars
            .AsNoTracking()
            .Include(c => c.Company)
            .FirstOrDefaultAsync(c => c.CarId == id);
    }

    // Tracked, with the company loaded
    public async Task<Car?> GetByModelNameAsync(string modelName)
    {
        return await _context.Cars
            .Include(c => c.Company)
            .FirstOrDefaultAsync(c => c.ModelName == modelName);
    }

    public async Task<bool> ExistsByModelNameAsync(string modelName)
    {
        return await _context.Cars.AnyAsync(c => c.ModelName == modelName);
    }

    // Read-only, active cars of active companies
    public async Task<List<Car>> GetActiveAsync()
    {
        return await _context.Cars
            .AsNoTracking()
            .Include(c => c.Company)
            .Where(c => c.IsActive && c.Company.IsActive)
            .OrderBy(c => c.CarId)
            .ToListAsync();
    }

    // Read-only, cars in maintenance (active or not)
    public async Task<List<Car>> GetMaintenanceAsync()
    {
        return await _context.Cars
            .AsNoTracking()
            .Include(c => c.Company)
            .Where(c => c.RentalStatus == RentalStatus.MAINTENANCE)
            .OrderBy(c => c.CarId)
            .ToListAsync();
    }

    // Read-only, every car (active or not) with its company, for the admin analyst
    public async Task<List<Car>> GetAllWithCompanyAsync()
    {
        return await _context.Cars
            .AsNoTracking()
            .Include(c => c.Company)
            .OrderBy(c => c.CarId)
            .ToListAsync();
    }

    // Read-only, cars that have at least one booking with this status
    public async Task<List<Car>> GetByBookingStatusAsync(BookingStatus status)
    {
        return await _context.Cars
            .AsNoTracking()
            .Include(c => c.Company)
            .Where(c => _context.Bookings.Any(b => b.CarId == c.CarId && b.BookingStatus == status))
            .OrderBy(c => c.CarId)
            .ToListAsync();
    }

    // false = the unique index on model_name rejected it (two saves at the same moment)
    public async Task<bool> AddAsync(Car car)
    {
        _context.Cars.Add(car);

        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }

    // The car must come from this repository (tracked)
    public async Task UpdateAsync(Car car)
    {
        await _context.SaveChangesAsync();
    }
}
