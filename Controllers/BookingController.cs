using DriveX.DTOs;
using DriveX.DTOs.Bookings;
using DriveX.DTOs.Cars;
using DriveX.Extensions;
using DriveX.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DriveX.Controllers;

[ApiController]
[Route("api/booking")]
public class BookingController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    // A customer books a car for themselves. The user comes from the token, not from the request body.
    [Authorize(Roles = "USER")]
    [HttpPost]
    public async Task<ActionResult<BookingResultResponse>> CreateBooking(CreateBookingRequest request)
    {
        return Ok(await _bookingService.CreateAsync(request, User.GetUserId()));
    }

    // A customer sees their own bookings. ADMIN or STUFF may look up any customer.
    // The page sends ?email=..., so it is checked against the email inside the token:
    // user a@x.com can not read the bookings of b@x.com just by changing the URL.
    [Authorize(Roles = "USER,ADMIN,STUFF")]
    [HttpGet("all-booking-by-email")]
    public async Task<ActionResult<List<BookingResponse>>> GetAllBookingsByEmail([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new MessageResponse("email is required"));
        }

        if (!User.IsStaff() && !string.Equals(User.GetEmail(), email.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new MessageResponse("You can only view your own bookings"));
        }

        return Ok(await _bookingService.GetByEmailAsync(email));
    }

    // ---- Staff pages: hand the car over and finish the rental time (ADMIN or STUFF) ----

    // Cars that have a booking waiting to be handed over.
    [Authorize(Roles = "ADMIN,STUFF")]
    [HttpGet("get-all-not_delivered_car")]
    public async Task<ActionResult<List<CarResponse>>> GetAllNotDeliveredCars()
    {
        return Ok(await _bookingService.GetNotDeliveredCarsAsync());
    }

    // The waiting booking of the car the staff member picked (by model name).
    [Authorize(Roles = "ADMIN,STUFF")]
    [HttpPost("get-all-bookings-not_delivered")]
    public async Task<ActionResult<BookingResponse>> GetNotDeliveredBooking(GetNotDeliveredBookingRequest request)
    {
        return Ok(await _bookingService.GetNotDeliveredBookingAsync(request.ModelName));
    }

    [Authorize(Roles = "ADMIN,STUFF")]
    [HttpPatch("set-rent-start-time/{bookingId:long}")]
    public async Task<ActionResult<BookingResponse>> SetRentStartTime(long bookingId)
    {
        return Ok(await _bookingService.StartRentAsync(bookingId));
    }

    [Authorize(Roles = "ADMIN,STUFF")]
    [HttpPatch("set-rent-end/{bookingId:long}")]
    public async Task<ActionResult<BookingResponse>> SetRentEnd(long bookingId)
    {
        return Ok(await _bookingService.EndRentAsync(bookingId));
    }

    // ---- Staff page: return and submit a car (ADMIN or STUFF) ----

    // Cars that are out with a customer right now.
    [Authorize(Roles = "ADMIN,STUFF")]
    [HttpGet("get-all-ongoing-cars")]
    public async Task<ActionResult<List<CarResponse>>> GetAllOngoingCars()
    {
        return Ok(await _bookingService.GetOngoingCarsAsync());
    }

    // The running booking of the car the staff member picked.
    [Authorize(Roles = "ADMIN,STUFF")]
    [HttpPost("get-booking-of-car")]
    public async Task<ActionResult<BookingResponse>> GetBookingOfCar(GetBookingOfCarRequest request)
    {
        return Ok(await _bookingService.GetOngoingBookingOfCarAsync(request.CarId!.Value));
    }

    [Authorize(Roles = "ADMIN,STUFF")]
    [HttpPatch("set-booking-car-submitted/{bookingId:long}")]
    public async Task<ActionResult<BookingResponse>> SetBookingCarSubmitted(long bookingId)
    {
        return Ok(await _bookingService.MarkSubmittedAsync(bookingId));
    }
}
