using DriveX.Data;
using DriveX.Models;
using Microsoft.EntityFrameworkCore;

namespace DriveX.Repositories;

public class ConfirmationCodeRepository : IConfirmationCodeRepository
{
    private readonly DriveXDbContext _context;

    public ConfirmationCodeRepository(DriveXDbContext context)
    {
        _context = context;
    }

    // Tracked, because verifying the code marks it as used
    public async Task<ConfirmationCode?> GetLatestUnusedAsync(string email)
    {
        return await _context.ConfirmationCodes
            .Where(c => c.Email == email && c.IsUsed == false)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync();
    }

    // Read-only, the newest verified code
    public async Task<ConfirmationCode?> GetLatestUsedAsync(string email)
    {
        return await _context.ConfirmationCodes
            .AsNoTracking()
            .Where(c => c.Email == email && c.IsUsed == true)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync();
    }

    // Read-only, the newest 'take' unverified codes (used for the "5 codes per 20 minutes" limit)
    public async Task<List<ConfirmationCode>> GetRecentUnusedAsync(string email, int take)
    {
        return await _context.ConfirmationCodes
            .AsNoTracking()
            .Where(c => c.Email == email && c.IsUsed == false)
            .OrderByDescending(c => c.CreatedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<ConfirmationCode> AddAsync(ConfirmationCode code)
    {
        _context.ConfirmationCodes.Add(code);
        await _context.SaveChangesAsync();
        return code;
    }

    // The code must come from this repository (tracked)
    public async Task UpdateAsync(ConfirmationCode code)
    {
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(ConfirmationCode code)
    {
        _context.ConfirmationCodes.Remove(code);
        await _context.SaveChangesAsync();
    }

    // Removes every code of this email in one query
    public async Task DeleteAllByEmailAsync(string email)
    {
        await _context.ConfirmationCodes
            .Where(c => c.Email == email)
            .ExecuteDeleteAsync();
    }
}
