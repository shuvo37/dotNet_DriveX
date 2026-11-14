using DriveX.Data;
using DriveX.Enums;
using DriveX.Models;
using Microsoft.EntityFrameworkCore;

namespace DriveX.Repositories;

public class CompanyRepository : ICompanyRepository
{
    private readonly DriveXDbContext _context;

    public CompanyRepository(DriveXDbContext context)
    {
        _context = context;
    }

    // Tracked, so the service can change it and call UpdateAsync
    public async Task<Company?> GetByIdAsync(long id)
    {
        return await _context.Companies.FirstOrDefaultAsync(c => c.CompanyId == id);
    }

    // Tracked
    public async Task<Company?> GetByNameAsync(string name)
    {
        return await _context.Companies.FirstOrDefaultAsync(c => c.CompanyName == name);
    }

    // excludeCompanyId lets a company keep its own name when it is updated
    public async Task<bool> ExistsByNameAsync(string name, long? excludeCompanyId = null)
    {
        return await _context.Companies
            .AnyAsync(c => c.CompanyName == name && c.CompanyId != excludeCompanyId);
    }

    public async Task<bool> ExistsByEmailAsync(string email, long? excludeCompanyId = null)
    {
        return await _context.Companies
            .AnyAsync(c => c.CompanyEmail == email && c.CompanyId != excludeCompanyId);
    }

    // Read-only, active companies only
    public async Task<List<Company>> GetActiveAsync()
    {
        return await _context.Companies
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.CompanyId)
            .ToListAsync();
    }

    // Read-only, companies with no RENTED or AVAILABLE car (active or not)
    public async Task<List<Company>> GetWithoutRentableCarsAsync()
    {
        return await _context.Companies
            .AsNoTracking()
            .Where(c => !_context.Cars.Any(car =>
                car.CompanyId == c.CompanyId &&
                (car.RentalStatus == RentalStatus.RENTED || car.RentalStatus == RentalStatus.AVAILABLE)))
            .OrderBy(c => c.CompanyId)
            .ToListAsync();
    }

    // false = the unique index on name/email rejected it (two saves at the same moment)
    public async Task<bool> AddAsync(Company company)
    {
        _context.Companies.Add(company);
        return await TrySaveAsync();
    }

    // The company must come from this repository (tracked). false = unique index rejected it.
    public async Task<bool> UpdateAsync(Company company)
    {
        return await TrySaveAsync();
    }

    private async Task<bool> TrySaveAsync()
    {
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
}
