using DriveX.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DriveX.Controllers;

// TEMPORARY: proves the database connection and the entity mappings work. We delete it later.
[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly DriveXDbContext _db;

    public HealthController(DriveXDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        if (!await _db.Database.CanConnectAsync())
        {
            return StatusCode(503, new { database = "unreachable" });
        }

        var counts = new
        {
            users = await _db.Users.CountAsync(),
            companies = await _db.Companies.CountAsync(),
            cars = await _db.Cars.CountAsync(),
            payments = await _db.Payments.CountAsync(),
            bookings = await _db.Bookings.CountAsync(),
            submissions = await _db.Submissions.CountAsync(),
            confirmationCodes = await _db.ConfirmationCodes.CountAsync()
        };

        // Loading real rows forces EF Core to read every mapped column.
        var firstBooking = await _db.Bookings.AsNoTracking()
                                .Include(b => b.Car).Include(b => b.Payment).Include(b => b.User)
                                .OrderBy(b => b.BookingId).FirstOrDefaultAsync();
        var firstSubmission = await _db.Submissions.AsNoTracking()
                                .Include(s => s.Booking).Include(s => s.Payment)
                                .Include(s => s.Car).Include(s => s.User)
                                .OrderBy(s => s.SubmissionId).FirstOrDefaultAsync();
        var firstCode = await _db.ConfirmationCodes.AsNoTracking()
                                .OrderBy(c => c.Id).FirstOrDefaultAsync();

        return Ok(new
        {
            database = "connected",
            counts,
            firstBookingCar = firstBooking?.Car.ModelName,
            firstBookingStatus = firstBooking?.BookingStatus?.ToString(),
            firstSubmissionCar = firstSubmission?.Car.ModelName,
            firstSubmissionBookingId = firstSubmission?.BookingId,
            firstCodeIsUsed = firstCode?.IsUsed
        });
    }
}
