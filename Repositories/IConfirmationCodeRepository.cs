using DriveX.Models;

namespace DriveX.Repositories;

// "Unused" = the code was sent but not verified yet. "Used" = the code was verified
// and may now be exchanged for a new password.
public interface IConfirmationCodeRepository
{
    Task<ConfirmationCode?> GetLatestUnusedAsync(string email);
    Task<ConfirmationCode?> GetLatestUsedAsync(string email);
    Task<List<ConfirmationCode>> GetRecentUnusedAsync(string email, int take);
    Task<ConfirmationCode> AddAsync(ConfirmationCode code);
    Task UpdateAsync(ConfirmationCode code);
    Task DeleteAsync(ConfirmationCode code);
    Task DeleteAllByEmailAsync(string email);
}
