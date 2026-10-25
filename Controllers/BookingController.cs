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
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<BookingResultResponse>> CreateBooking(CreateBookingRequest request)
    {
        return Ok(await _bookingService.CreateAsync(request, User.GetUserId()));
    }

    // All bookings of the customer with this email (same as Spring).
    [HttpGet("all-booking-by-email")]
    public async Task<ActionResult<List<BookingResponse>>> GetAllBookingsByEmail([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new MessageResponse("email is required"));
        }

        return Ok(await _bookingService.GetByEmailAsync(email));
    }

    // ---- Staff pages: hand the car over and finish the rental time ----

    // Cars that have a booking waiting to be handed over.
    [HttpGet("get-all-not_delivered_car")]
    public async Task<ActionResult<List<CarResponse>>> GetAllNotDeliveredCars()
    {
        return Ok(await _bookingService.GetNotDeliveredCarsAsync());
    }

    // The waiting booking of the car the staff member picked (by model name).
    [HttpPost("get-all-bookings-not_delivered")]
    public async Task<ActionResult<BookingResponse>> GetNotDeliveredBooking(GetNotDeliveredBookingRequest request)
    {
        return Ok(await _bookingService.GetNotDeliveredBookingAsync(request.ModelName));
    }

    [HttpPatch("set-rent-start-time/{bookingId:long}")]
    public async Task<ActionResult<BookingResponse>> SetRentStartTime(long bookingId)
    {
        return Ok(await _bookingService.StartRentAsync(bookingId));
    }

    [HttpPatch("set-rent-end/{bookingId:long}")]
    public async Task<ActionResult<BookingResponse>> SetRentEnd(long bookingId)
    {
        return Ok(await _bookingService.EndRentAsync(bookingId));
    }

    // ---- Staff page: return and submit a car ----

    // Cars that are out with a customer right now.
    [HttpGet("get-all-ongoing-cars")]
    public async Task<ActionResult<List<CarResponse>>> GetAllOngoingCars()
    {
        return Ok(await _bookingService.GetOngoingCarsAsync());
    }

    // The running booking of the car the staff member picked.
    [HttpPost("get-booking-of-car")]
    public async Task<ActionResult<BookingResponse>> GetBookingOfCar(GetBookingOfCarRequest request)
    {
        return Ok(await _bookingService.GetOngoingBookingOfCarAsync(request.CarId!.Value));
    }

    [HttpPatch("set-booking-car-submitted/{bookingId:long}")]
    public async Task<ActionResult<BookingResponse>> SetBookingCarSubmitted(long bookingId)
    {
        return Ok(await _bookingService.MarkSubmittedAsync(bookingId));
    }

    // Like Spring: returns the running booking of the car with this model name,
    // or an empty response when that car has no running booking (ASP.NET sends 204 for null).
    [HttpGet("search-Booking-Car")]
    public async Task<ActionResult<BookingResponse?>> SearchBookingCar([FromQuery] string modelName)
    {
        return Ok(await _bookingService.SearchOngoingBookingByModelAsync(modelName));
    }
}
