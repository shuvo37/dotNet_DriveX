using DriveX.DTOs;
using DriveX.DTOs.Cars;
using DriveX.Enums;
using DriveX.Exceptions;
using DriveX.Mapping;
using DriveX.Models;
using DriveX.Repositories;

namespace DriveX.Services;

public class CarService : ICarService
{
    private readonly ICarRepository _cars;
    private readonly ICompanyRepository _companies;

    public CarService(ICarRepository cars, ICompanyRepository companies)
    {
        _cars = cars;
        _companies = companies;
    }

    public async Task<CarResultResponse> AddCarAsync(AddCarRequest request)
    {
        var modelName = request.ModelName.Trim();
        var companyName = request.CompanyName.Trim();

        if (await _cars.ExistsByModelNameAsync(modelName))
        {
            throw new AppException("Car already exists");
        }

        var company = await _companies.GetByNameAsync(companyName)
            ?? throw new AppException("Company not found");

        var car = new Car
        {
            Company = company,
            ModelName = modelName,
            PricePerHour = request.PricePerHour!.Value,
            RentalStatus = request.RentalStatus!.Value,
            Seats = request.Seats!.Value,
            Fuel = request.Fuel!.Value,
            ImageUrl = request.ImageUrl.Trim(),
            Tag = CleanTag(request.Tag),
            IsActive = true
        };

        // The unique index on model_name also protects against two saves at the same moment.
        if (!await _cars.AddAsync(car))
        {
            throw new AppException("Car already exists");
        }

        return new CarResultResponse("Car added successfully", car.ToResponse());
    }

    public async Task<CarResultResponse> UpdateCarAsync(long carId, UpdateCarRequest request)
    {
        var car = await _cars.GetByIdAsync(carId)
            ?? throw new AppException("Car not found");

        car.PricePerHour = request.PricePerHour!.Value;
        car.RentalStatus = request.RentalStatus!.Value;
        car.Seats = request.Seats!.Value;
        car.Fuel = request.Fuel!.Value;
        car.ImageUrl = request.ImageUrl.Trim();
        car.Tag = CleanTag(request.Tag);

        await _cars.UpdateAsync(car);

        return new CarResultResponse("Car updated successfully", car.ToResponse());
    }

    // Customers only see active cars of active companies (same as Spring).
    public async Task<List<CarResponse>> GetActiveCarsAsync()
    {
        var cars = await _cars.GetActiveAsync();

        return cars.Select(x => x.ToResponse()).ToList();
    }

    // Cars that are in maintenance, active or not. The admin "car active status" page lists these.
    public async Task<List<CarResponse>> GetMaintenanceCarsAsync()
    {
        var cars = await _cars.GetMaintenanceAsync();

        return cars.Select(x => x.ToResponse()).ToList();
    }

    public async Task<MessageResponse> UpdateRentalStatusAsync(UpdateCarRentalStatusRequest request)
    {
        var carId = request.CarId!.Value;

        var car = await _cars.GetByIdAsync(carId)
            ?? throw new AppException("Car not found");

        car.RentalStatus = request.RentalStatus!.Value;
        await _cars.UpdateAsync(car);

        return new MessageResponse("Car rental status updated successfully");
    }

    // Used when a returned car is submitted: the car goes to maintenance.
    public async Task<MessageResponse> MarkAsMaintenanceAsync(long carId)
    {
        var car = await _cars.GetByIdAsync(carId)
            ?? throw new AppException("Car not found");

        car.RentalStatus = RentalStatus.MAINTENANCE;
        await _cars.UpdateAsync(car);

        return new MessageResponse("Car rental status updated successfully");
    }

    // The React page identifies the car by model name, as it did with Spring.
    public async Task<CarResultResponse> UpdateActiveStatusAsync(UpdateCarActiveStatusRequest request)
    {
        var modelName = request.ModelName.Trim();

        var car = await _cars.GetByModelNameAsync(modelName)
            ?? throw new AppException("Car not found");

        car.IsActive = request.IsActive!.Value;
        await _cars.UpdateAsync(car);

        return new CarResultResponse("Car updated successfully", car.ToResponse());
    }

    private static string? CleanTag(string? tag) =>
        string.IsNullOrWhiteSpace(tag) ? null : tag.Trim();
}
