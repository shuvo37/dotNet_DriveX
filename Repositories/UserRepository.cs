using DriveX.Data;
using DriveX.Models;
using Microsoft.EntityFrameworkCore;

namespace DriveX.Repositories;

public class UserRepository : IUserRepository
{
    private readonly DriveXDbContext _context;

    public UserRepository(DriveXDbContext context)
    {
        _context = context;
    }

    // Tracked, so the service can change it and call UpdateAsync
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    // Read-only (login, profile lookups)
    public async Task<User?> GetDetailsByEmailAsync(string email)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    // Read-only, by id
    public async Task<User?> GetDetailsByIdAsync(long id)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == id);
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email == email);
    }

    public async Task<User> AddAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    // The user must come from this repository (tracked)
    public async Task UpdateAsync(User user)
    {
        await _context.SaveChangesAsync();
    }
}
