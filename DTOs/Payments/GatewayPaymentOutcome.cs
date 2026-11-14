namespace DriveX.DTOs.Payments;

// The result of checking a payment with SSLCommerz. PaymentId is set only when IsPaid is true.
public record GatewayPaymentOutcome(bool IsPaid, long? PaymentId);
