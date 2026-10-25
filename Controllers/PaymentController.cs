using DriveX.DTOs.Payments;
using DriveX.Extensions;
using DriveX.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DriveX.Controllers;

[ApiController]
[Route("api/payment")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    // Records a payment. The caller comes from the token: a customer pays their own rental price,
    // staff record a fine for the customer in the request. The service handles which is which.
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<PaymentResultResponse>> CreatePayment(CreatePaymentRequest request)
    {
        var response = await _paymentService.CreateAsync(request, User.GetUserId(), User.IsStaff());
        return Ok(response);
    }
}
