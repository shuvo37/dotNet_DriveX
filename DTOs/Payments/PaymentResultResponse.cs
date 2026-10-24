namespace DriveX.DTOs.Payments;

// { "message": "...", "payment": { ... } }. React reads response.data.payment.paymentId.
public record PaymentResultResponse(string Message, PaymentResponse Payment);
