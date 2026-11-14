namespace DriveX.Options;

// Bound from the "SslCommerz" section of configuration.
// StoreId and StorePassword come from user-secrets, never from appsettings.json.
public class SslCommerzSettings
{
    public string StoreId { get; set; } = string.Empty;
    public string StorePassword { get; set; } = string.Empty;

    // true = sandbox (test money). Set to false only with a live store id.
    public bool IsSandbox { get; set; } = true;

    // Public address of this backend. SSLCommerz sends the customer's browser back to it.
    public string BackendBaseUrl { get; set; } = "http://localhost:8080";

    // The number shown as "$" in the React app is charged in BDT.
    public string Currency { get; set; } = "BDT";

    // SSLCommerz does not accept a payment below this amount (in BDT).
    public const double MinimumAmount = 10.00;

    public string GatewayBaseUrl => IsSandbox
        ? "https://sandbox.sslcommerz.com"
        : "https://securepay.sslcommerz.com";
}
