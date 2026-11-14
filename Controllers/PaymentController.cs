using DriveX.DTOs.Payments;
using DriveX.Exceptions;
using DriveX.Extensions;
using DriveX.Options;
using DriveX.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DriveX.Controllers;

[ApiController]
[Route("api/payment")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly FrontendSettings _frontend;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        IPaymentService paymentService,
        IOptions<FrontendSettings> frontend,
        ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _frontend = frontend.Value;
        _logger = logger;
    }

    // Starts a real payment: creates a PENDING payment and returns the SSLCommerz page to pay on.
    // The customer is the logged-in user. Price and status are decided by the server.
    [Authorize]
    [HttpPost("sslcommerz/start")]
    public async Task<ActionResult<StartPaymentResponse>> StartGatewayPayment(StartPaymentRequest request)
    {
        var response = await _paymentService.StartGatewayPaymentAsync(request, User.GetUserId());
        return Ok(response);
    }

    // Staff pay a customer's late fine. The amount is worked out by the server from the booking.
    [Authorize]
    [HttpPost("sslcommerz/fine/start")]
    public async Task<ActionResult<StartPaymentResponse>> StartFineGatewayPayment(StartFinePaymentRequest request)
    {
        var response = await _paymentService.StartFineGatewayPaymentAsync(request, User.IsStaff());
        return Ok(response);
    }

    // ---- Where SSLCommerz sends the browser back to ----
    // These have no login token: the browser arrives from another website, so there is no
    // Authorization header. That is fine because nothing here trusts the browser. Success is checked
    // with SSLCommerz itself, and every answer is a redirect to a React page that shows the result.
    // Rental payments land on the customer page. Fine payments land on the staff page.

    [HttpPost("sslcommerz/success")]
    public Task<IActionResult> GatewaySuccess(
        [FromForm(Name = "tran_id")] string? transactionId,
        [FromForm(Name = "val_id")] string? valId) =>
        ConfirmAndRedirect(transactionId, valId, RentalResultUrl);

    // Fail and cancel only send the browser to a message page. They change nothing in the database:
    // these addresses are open to anyone, and a stranger must not be able to fail someone's payment.
    [HttpPost("sslcommerz/fail")]
    public IActionResult GatewayFail() => Redirect(RentalResultUrl("failed"));

    [HttpPost("sslcommerz/cancel")]
    public IActionResult GatewayCancel() => Redirect(RentalResultUrl("cancelled"));

    [HttpPost("sslcommerz/fine/success")]
    public Task<IActionResult> FineGatewaySuccess(
        [FromForm(Name = "tran_id")] string? transactionId,
        [FromForm(Name = "val_id")] string? valId) =>
        ConfirmAndRedirect(transactionId, valId, FineResultUrl);

    [HttpPost("sslcommerz/fine/fail")]
    public IActionResult FineGatewayFail() => Redirect(FineResultUrl("failed"));

    [HttpPost("sslcommerz/fine/cancel")]
    public IActionResult FineGatewayCancel() => Redirect(FineResultUrl("cancelled"));

    // Asks SSLCommerz if the money arrived, then redirects to the page that fits the answer.
    private async Task<IActionResult> ConfirmAndRedirect(
        string? transactionId, string? valId, Func<string, long?, string> resultUrl)
    {
        try
        {
            var outcome = await _paymentService.ConfirmGatewayPaymentAsync(transactionId, valId);

            return outcome.IsPaid
                ? Redirect(resultUrl("success", outcome.PaymentId))
                : Redirect(resultUrl("failed", null));
        }
        catch (AppException ex)
        {
            // We could not reach SSLCommerz to check. Money may have been taken, so this is not "failed".
            _logger.LogError(ex, "Could not confirm payment {TranId}", transactionId);
            return Redirect(resultUrl("unconfirmed", null));
        }
    }

    private string RentalResultUrl(string status, long? paymentId = null) =>
        ResultUrl("/user-home/payment-result", status, paymentId);

    private string FineResultUrl(string status, long? paymentId = null) =>
        ResultUrl("/stuff-home/fine-result", status, paymentId);

    private string ResultUrl(string path, string status, long? paymentId)
    {
        var url = $"{_frontend.BaseUrl.TrimEnd('/')}{path}?status={status}";
        return paymentId is null ? url : $"{url}&paymentId={paymentId}";
    }
}
