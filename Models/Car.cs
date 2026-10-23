using DriveX.Enums;

namespace DriveX.Models;

// Maps to the existing "cars" table. Each car belongs to one company.
public class Car
{
    public long CarId { get; set; }

    public long CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public string ModelName { get; set; } = string.Empty;
    public double PricePerHour { get; set; }
    public RentalStatus RentalStatus { get; set; }
    public int Seats { get; set; }
    public FuelType Fuel { get; set; }
    public string? ImageUrl { get; set; }
    public string? Tag { get; set; }
    public bool IsActive { get; set; }
}
