using DriveX.Models;

namespace DriveX.Repositories;

public interface ISubmissionRepository
{
    Task<bool> ExistsByBookingIdAsync(long bookingId);
    Task<bool> AddAsync(Submission submission);
}
