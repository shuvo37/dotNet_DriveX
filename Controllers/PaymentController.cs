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

    // USER pays a rental price; ADMIN or STUFF record a fine. The service enforces which is which.
    [Authorize(Roles = "USER,ADMIN,STUFF")]
    [HttpPost]
    public async Task<ActionResult<PaymentResultResponse>> CreatePayment(CreatePaymentRequest request)
    {
        var response = await _paymentService.CreateAsync(request, User.GetUserId(), User.IsStaff());
        return Ok(response);
    }
}
