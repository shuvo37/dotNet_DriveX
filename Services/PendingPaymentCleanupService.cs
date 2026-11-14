using DriveX.Data;
using DriveX.Enums;
using Microsoft.EntityFrameworkCore;

namespace DriveX.Services;

// Runs in the background for as long as the backend is running.
// A customer who opens the SSLCommerz page and then closes it leaves a PENDING payment behind.
// Every 10 minutes this marks gateway payments (rentals and fines) that stayed PENDING for more than 1 hour as FAILED.
// Example: a payment started at 14:05 and never finished becomes FAILED at the first run after 15:05.
// A customer who pays late is still fine: confirming a payment asks SSLCommerz and sets DONE
// whatever the old status was, so FAILED here never blocks a real payment.
public class PendingPaymentCleanupService : BackgroundService
{
    private static readonly TimeSpan RunEvery = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan AbandonedAfter = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PendingPaymentCleanupService> _logger;

    public PendingPaymentCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<PendingPaymentCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(RunEvery);

        try
        {
            do
            {
                await CleanUpAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // The backend is shutting down.
        }
    }

    private async Task CleanUpAsync(CancellationToken stoppingToken)
    {
        try
        {
            // A background service lives forever, but the DbContext must not, so each run gets its own.
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DriveXDbContext>();

            var cutoff = DateTime.Now - AbandonedAfter; // payment_time is stored in local time

            // Only payments that went through the gateway have a transaction id. Nothing else is touched.
            var changed = await db.Payments
                .Where(p => p.PaymentStatus == PaymentStatus.PENDING
                            && p.TransactionId != null
                            && p.PaymentTime < cutoff)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.PaymentStatus, PaymentStatus.FAILED), stoppingToken);

            if (changed > 0)
            {
                _logger.LogInformation("Marked {Count} abandoned PENDING payments as FAILED", changed);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A database hiccup must not stop the backend. The next run tries again.
            _logger.LogError(ex, "Could not clean up PENDING payments");
        }
    }
}
