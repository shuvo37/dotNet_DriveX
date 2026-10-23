using DriveX.Enums;

namespace DriveX.Models;

// Maps to the existing "payment" table (singular name, as Hibernate created it).
public class Payment
{
    public long PaymentId { get; set; }

    public long CarId { get; set; }
    public Car Car { get; set; } = null!;

    public long UserId { get; set; }
    public User User { get; set; } = null!;

    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public PaymentType PaymentType { get; set; }
    public double TotalAmount { get; set; }
    public DateTime PaymentTime { get; set; }
}
