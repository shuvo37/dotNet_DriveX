using DriveX.DTOs.Payments;

namespace DriveX.Services;

public interface IPaymentService
{
    // Creates a PENDING rental payment and returns the SSLCommerz page to pay on.
    Task<StartPaymentResponse> StartGatewayPaymentAsync(StartPaymentRequest request, long userId);

    // Staff pay a late fine on SSLCommerz. Creates a PENDING FINE payment and returns the page to pay on.
    Task<StartPaymentResponse> StartFineGatewayPaymentAsync(StartFinePaymentRequest request, bool callerIsStaff);

    // Called when the customer comes back from SSLCommerz. Asks SSLCommerz if the money really arrived
    // and only then sets the payment to DONE.
    Task<GatewayPaymentOutcome> ConfirmGatewayPaymentAsync(string? transactionId, string? valId);
}
