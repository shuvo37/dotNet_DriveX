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

    // Our own unique id for one gateway payment (sent to SSLCommerz as tran_id, sent back by it).
    // Example: "DRX-7-12-A1B2C3D4". Null for old payments and for staff fine payments.
    public string? TransactionId { get; set; }
}
