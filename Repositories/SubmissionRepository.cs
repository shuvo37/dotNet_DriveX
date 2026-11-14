using DriveX.Data;
using DriveX.Models;
using Microsoft.EntityFrameworkCore;

namespace DriveX.Repositories;

public class SubmissionRepository : ISubmissionRepository
{
    private readonly DriveXDbContext _context;

    public SubmissionRepository(DriveXDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsByBookingIdAsync(long bookingId)
    {
        return await _context.Submissions.AnyAsync(s => s.BookingId == bookingId);
    }

    // false = the unique indexes on booking_id / payment_id rejected it (a double click)
    public async Task<bool> AddAsync(Submission submission)
    {
        _context.Submissions.Add(submission);

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
