using DriveX.Models;
using Microsoft.EntityFrameworkCore;

namespace DriveX.Data;

// The bridge between C# and the MySQL "drivex" database.
public class DriveXDbContext : DbContext
{
    public DriveXDbContext(DbContextOptions<DriveXDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Car> Cars => Set<Car>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<ConfirmationCode> ConfirmationCodes => Set<ConfirmationCode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Picks up every IEntityTypeConfiguration<T> class in this project.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DriveXDbContext).Assembly);
    }
}
