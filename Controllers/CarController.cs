using DriveX.DTOs;
using DriveX.DTOs.Cars;
using DriveX.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DriveX.Controllers;

[ApiController]
[Route("api/car")]
public class CarController : ControllerBase
{
    private readonly ICarService _carService;

    public CarController(ICarService carService)
    {
        _carService = carService;
    }

    [HttpPost]
    public async Task<ActionResult<CarResultResponse>> AddCar(AddCarRequest request)
    {
        return Ok(await _carService.AddCarAsync(request));
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<CarResultResponse>> UpdateCar(long id, UpdateCarRequest request)
    {
        return Ok(await _carService.UpdateCarAsync(id, request));
    }

    // Public: customers browse cars. Only active cars of active companies are returned.
    [HttpGet]
    public async Task<ActionResult<List<CarResponse>>> GetCars()
    {
        return Ok(await _carService.GetActiveCarsAsync());
    }

    // Cars in maintenance (used by the admin car-status page).
    [HttpGet("all")]
    public async Task<ActionResult<List<CarResponse>>> GetMaintenanceCars()
    {
        return Ok(await _carService.GetMaintenanceCarsAsync());
    }

    // Sets the rental status of a car (the staff pages use it).
    [HttpPatch("updateCarRentalStatus")]
    public async Task<ActionResult<MessageResponse>> UpdateCarRentalStatus(UpdateCarRentalStatusRequest request)
    {
        return Ok(await _carService.UpdateRentalStatusAsync(request));
    }

    // Used by the staff page when a returned car is submitted: sets the car to MAINTENANCE.
    [HttpPatch("updateCarRentalStatus/{carId:long}")]
    public async Task<ActionResult<MessageResponse>> MarkCarAsMaintenance(long carId)
    {
        return Ok(await _carService.MarkAsMaintenanceAsync(carId));
    }

    [HttpPatch("update-Active-Status")]
    public async Task<ActionResult<CarResultResponse>> UpdateCarActiveStatus(UpdateCarActiveStatusRequest request)
    {
        return Ok(await _carService.UpdateActiveStatusAsync(request));
    }
}
