namespace DriveX.DTOs.Payments;

// The frontend sends the customer's browser to GatewayUrl, the SSLCommerz page where they pay.
public record StartPaymentResponse(long PaymentId, string GatewayUrl);
