using DriveX.Enums;
using DriveX.Models;

namespace DriveX.Repositories;

public interface ICarRepository
{
    Task<Car?> GetByIdAsync(long id);
    Task<Car?> GetDetailsByIdAsync(long id);
    Task<Car?> GetByModelNameAsync(string modelName);
    Task<bool> ExistsByModelNameAsync(string modelName);
    Task<List<Car>> GetActiveAsync();
    Task<List<Car>> GetMaintenanceAsync();
    Task<List<Car>> GetAllWithCompanyAsync();
    Task<List<Car>> GetByBookingStatusAsync(BookingStatus status);
    Task<bool> AddAsync(Car car);
    Task UpdateAsync(Car car);
}
