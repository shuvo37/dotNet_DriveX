using DriveX.Enums;

namespace DriveX.DTOs.Payments;

// What the API returns for a payment: plain ids, never the User or Car entities.
public record PaymentResponse(
    long PaymentId,
    long CarId,
    long UserId,
    PaymentMethod PaymentMethod,
    PaymentStatus PaymentStatus,
    PaymentType PaymentType,
    double TotalAmount,
    DateTime PaymentTime);
