using DriveX.Data;
using DriveX.Enums;
using DriveX.Models;
using Microsoft.EntityFrameworkCore;

namespace DriveX.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly DriveXDbContext _context;

    public PaymentRepository(DriveXDbContext context)
    {
        _context = context;
    }

    // Read-only, by id
    public async Task<Payment?> GetDetailsByIdAsync(long id)
    {
        return await _context.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PaymentId == id);
    }

    // Tracked, because confirming a payment changes its status
    public async Task<Payment?> GetByTransactionIdAsync(string transactionId)
    {
        return await _context.Payments.FirstOrDefaultAsync(p => p.TransactionId == transactionId);
    }

    // Read-only, DONE payments made in [start, endExclusive), with the car (for the admin analyst)
    public async Task<List<Payment>> GetDoneInRangeAsync(DateTime start, DateTime endExclusive, int take)
    {
        return await _context.Payments
            .AsNoTracking()
            .Include(p => p.Car)
            .Where(p => p.PaymentStatus == PaymentStatus.DONE
                        && p.PaymentTime >= start
                        && p.PaymentTime < endExclusive)
            .OrderBy(p => p.PaymentId)
            .Take(take)
            .ToListAsync();
    }

    public async Task<Payment> AddAsync(Payment payment)
    {
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
        return payment;
    }

    // The payment must come from this repository (tracked) or just have been added
    public async Task UpdateAsync(Payment payment)
    {
        await _context.SaveChangesAsync();
    }

    // Gateway payments (they have a transaction id) that stayed PENDING since before 'olderThan'
    // become FAILED, in one query. Returns how many were changed.
    public async Task<int> MarkAbandonedPendingAsFailedAsync(DateTime olderThan)
    {
        return await _context.Payments
            .Where(p => p.PaymentStatus == PaymentStatus.PENDING
                        && p.TransactionId != null
                        && p.PaymentTime < olderThan)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.PaymentStatus, PaymentStatus.FAILED));
    }
}
