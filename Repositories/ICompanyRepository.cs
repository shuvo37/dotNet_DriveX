using DriveX.Models;

namespace DriveX.Repositories;

public interface ICompanyRepository
{
    Task<Company?> GetByIdAsync(long id);
    Task<Company?> GetByNameAsync(string name);
    Task<bool> ExistsByNameAsync(string name, long? excludeCompanyId = null);
    Task<bool> ExistsByEmailAsync(string email, long? excludeCompanyId = null);
    Task<List<Company>> GetActiveAsync();
    Task<List<Company>> GetWithoutRentableCarsAsync();
    Task<bool> AddAsync(Company company);
    Task<bool> UpdateAsync(Company company);
}
