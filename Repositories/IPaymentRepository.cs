using DriveX.Models;

namespace DriveX.Repositories;

public interface IPaymentRepository
{
    Task<Payment?> GetDetailsByIdAsync(long id);
    Task<Payment?> GetByTransactionIdAsync(string transactionId);
    Task<List<Payment>> GetDoneInRangeAsync(DateTime start, DateTime endExclusive, int take);
    Task<Payment> AddAsync(Payment payment);
    Task UpdateAsync(Payment payment);
    Task<int> MarkAbandonedPendingAsFailedAsync(DateTime olderThan);
}
