namespace DriveX.Services;

// What we send to SSLCommerz to start one payment.
public record SslCommerzInitRequest(
    string TranId,
    decimal Amount,
    string CustomerName,
    string CustomerEmail,
    string CustomerPhone,
    string CustomerAddress,
    string CustomerCity,
    string ProductName,
    // Where SSLCommerz sends the browser back to, after this backend address. Fines use a different one.
    string CallbackPath = "/api/payment/sslcommerz");

// GatewayPageUrl is the SSLCommerz page the customer is sent to, to pay.
public record SslCommerzSession(string GatewayPageUrl, string SessionKey);

// SSLCommerz's own answer to "did this payment really succeed?".
// IsValid is true only for status VALID or VALIDATED. Amount and Currency are what we sent at the start.
public record SslCommerzValidation(
    bool IsValid,
    string Status,
    string TranId,
    decimal Amount,
    string Currency,
    string BankTranId);

public interface ISslCommerzClient
{
    Task<SslCommerzSession> InitiateAsync(SslCommerzInitRequest request);

    Task<SslCommerzValidation> ValidateAsync(string valId);
}
