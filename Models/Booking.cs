using DriveX.Enums;

namespace DriveX.Models;

// Maps to the existing "bookings" table. Named Booking (singular) in C#.
public class Booking
{
    public long BookingId { get; set; }

    public long UserId { get; set; }
    public User User { get; set; } = null!;

    public long PaymentId { get; set; }
    public Payment Payment { get; set; } = null!;

    public long CarId { get; set; }
    public Car Car { get; set; } = null!;

    public double TotalPrice { get; set; }
    public DateTime BookingTime { get; set; }
    public int Days { get; set; }
    public int Hour { get; set; }
    public int TotalHours { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public BookingStatus? BookingStatus { get; set; }
    public DateTime? RentStart { get; set; }
    public DateTime? RentEnd { get; set; }
    public bool Toggle { get; set; }
}
