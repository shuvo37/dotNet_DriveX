using DriveX.Models;

namespace DriveX.Repositories;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetDetailsByEmailAsync(string email);
    Task<User?> GetDetailsByIdAsync(long id);
    Task<bool> ExistsByEmailAsync(string email);
    Task<User> AddAsync(User user);
    Task UpdateAsync(User user);
}
