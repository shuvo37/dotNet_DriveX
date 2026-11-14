using System.Globalization;
using System.Text.Json;
using DriveX.Exceptions;
using DriveX.Options;
using Microsoft.Extensions.Options;

namespace DriveX.Services;

// Talks to SSLCommerz from the server (never from the browser, so nobody can tamper with it).
// Two calls: start a payment (get the page to send the customer to) and validate a finished one.
public class SslCommerzClient : ISslCommerzClient
{
    private readonly HttpClient _http;
    private readonly SslCommerzSettings _settings;
    private readonly ILogger<SslCommerzClient> _logger;

    public SslCommerzClient(HttpClient http, IOptions<SslCommerzSettings> options, ILogger<SslCommerzClient> logger)
    {
        _http = http;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<SslCommerzSession> InitiateAsync(SslCommerzInitRequest request)
    {
        EnsureConfigured();

        // The browser comes back to these backend addresses after paying, failing or cancelling.
        var callbackBase = _settings.BackendBaseUrl.TrimEnd('/') + request.CallbackPath;

        var form = new Dictionary<string, string>
        {
            ["store_id"] = _settings.StoreId,
            ["store_passwd"] = _settings.StorePassword,
            ["total_amount"] = request.Amount.ToString("F2", CultureInfo.InvariantCulture),
            ["currency"] = _settings.Currency,
            ["tran_id"] = request.TranId,
            ["success_url"] = $"{callbackBase}/success",
            ["fail_url"] = $"{callbackBase}/fail",
            ["cancel_url"] = $"{callbackBase}/cancel",

            ["cus_name"] = Limit(request.CustomerName, 50),
            ["cus_email"] = Limit(request.CustomerEmail, 50),
            ["cus_add1"] = Limit(request.CustomerAddress, 50),
            ["cus_city"] = Limit(request.CustomerCity, 50),
            ["cus_postcode"] = "1000",
            ["cus_country"] = "Bangladesh",
            ["cus_phone"] = Limit(request.CustomerPhone, 20),

            // A car rental has nothing to ship, but these fields are required.
            ["shipping_method"] = "NO",
            ["num_of_item"] = "1",
            ["product_name"] = Limit(request.ProductName, 255),
            ["product_category"] = "car rental",
            ["product_profile"] = "general"
        };

        var body = await SendAsync(() => _http.PostAsync(
            $"{_settings.GatewayBaseUrl}/gwprocess/v4/api.php", new FormUrlEncodedContent(form)));

        using var doc = ParseJson(body);
        var root = doc.RootElement;

        var status = GetString(root, "status");
        var gatewayUrl = GetString(root, "GatewayPageURL");

        if (status != "SUCCESS" || gatewayUrl == "")
        {
            // The reason is for us (wrong store id, missing field...), not for the customer.
            _logger.LogError("SSLCommerz refused to start payment {TranId}: {Reason}",
                request.TranId, GetString(root, "failedreason"));
            throw new AppException("Could not start the payment. Please try again.");
        }

        return new SslCommerzSession(gatewayUrl, GetString(root, "sessionkey"));
    }

    public async Task<SslCommerzValidation> ValidateAsync(string valId)
    {
        EnsureConfigured();

        var url = $"{_settings.GatewayBaseUrl}/validator/api/validationserverAPI.php" +
                  $"?val_id={Uri.EscapeDataString(valId)}" +
                  $"&store_id={Uri.EscapeDataString(_settings.StoreId)}" +
                  $"&store_passwd={Uri.EscapeDataString(_settings.StorePassword)}" +
                  "&v=1&format=json";

        var body = await SendAsync(() => _http.GetAsync(url));

        using var doc = ParseJson(body);
        var root = doc.RootElement;

        var status = GetString(root, "status");

        // currency_type / currency_amount are what we sent. "amount" is the BDT value after conversion.
        var currency = FirstNotEmpty(GetString(root, "currency_type"), GetString(root, "currency"));
        var amountText = FirstNotEmpty(GetString(root, "currency_amount"), GetString(root, "amount"));
        decimal.TryParse(amountText, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount);

        return new SslCommerzValidation(
            IsValid: status is "VALID" or "VALIDATED",
            Status: status,
            TranId: GetString(root, "tran_id"),
            Amount: amount,
            Currency: currency,
            BankTranId: GetString(root, "bank_tran_id"));
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_settings.StoreId) || string.IsNullOrWhiteSpace(_settings.StorePassword))
        {
            throw new InvalidOperationException(
                "SslCommerz:StoreId or SslCommerz:StorePassword is not configured. Set them with 'dotnet user-secrets set'.");
        }
    }

    // Sends one request and returns the response text. A network problem or a bad HTTP status
    // becomes a message the customer can read; the details go to the log.
    private async Task<string> SendAsync(Func<Task<HttpResponseMessage>> send)
    {
        try
        {
            using var response = await send();
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("SSLCommerz answered HTTP {Status}", (int)response.StatusCode);
                throw new AppException("The payment gateway is not available right now. Please try again.");
            }

            return body;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Could not reach SSLCommerz");
            throw new AppException("The payment gateway is not available right now. Please try again.");
        }
    }

    private JsonDocument ParseJson(string body)
    {
        try
        {
            return JsonDocument.Parse(body);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "SSLCommerz sent an answer that is not JSON");
            throw new AppException("The payment gateway sent an unexpected answer. Please try again.");
        }
    }

    private static string GetString(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value))
        {
            return string.Empty;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
            _ => value.ToString()
        };
    }

    // SSLCommerz rejects fields longer than its limits (for example cus_add1 is 50 characters).
    private static string Limit(string value, int max) =>
        value.Length <= max ? value : value[..max];

    private static string FirstNotEmpty(string first, string second) =>
        string.IsNullOrWhiteSpace(first) ? second : first;
}
