using DriveX.Data;
using DriveX.DTOs;
using DriveX.DTOs.Cars;
using DriveX.DTOs.Companies;
using DriveX.Enums;
using DriveX.Exceptions;
using DriveX.Models;
using Microsoft.EntityFrameworkCore;

namespace DriveX.Services;

public class CarService : ICarService
{
    private readonly DriveXDbContext _db;

    public CarService(DriveXDbContext db)
    {
        _db = db;
    }

    public async Task<CarResultResponse> AddCarAsync(AddCarRequest request)
    {
        var modelName = request.ModelName.Trim();
        var companyName = request.CompanyName.Trim();

        if (await _db.Cars.AnyAsync(c => c.ModelName == modelName))
        {
            throw new AppException("Car already exists");
        }

        var company = await _db.Companies.FirstOrDefaultAsync(c => c.CompanyName == companyName)
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

        _db.Cars.Add(car);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // The database has a unique index on model_name; this covers two saves at the same moment.
            throw new AppException("Car already exists");
        }

        return new CarResultResponse("Car added successfully", ToResponse(car));
    }

    public async Task<CarResultResponse> UpdateCarAsync(long carId, UpdateCarRequest request)
    {
        var car = await _db.Cars.Include(c => c.Company).FirstOrDefaultAsync(c => c.CarId == carId)
            ?? throw new AppException("Car not found");

        car.PricePerHour = request.PricePerHour!.Value;
        car.RentalStatus = request.RentalStatus!.Value;
        car.Seats = request.Seats!.Value;
        car.Fuel = request.Fuel!.Value;
        car.ImageUrl = request.ImageUrl.Trim();
        car.Tag = CleanTag(request.Tag);

        await _db.SaveChangesAsync();

        return new CarResultResponse("Car updated successfully", ToResponse(car));
    }

    // Customers only see active cars of active companies (same as Spring).
    public async Task<List<CarResponse>> GetActiveCarsAsync()
    {
        var cars = await _db.Cars
            .AsNoTracking()
            .Include(c => c.Company)
            .Where(c => c.IsActive && c.Company.IsActive)
            .OrderBy(c => c.CarId)
            .ToListAsync();

        return cars.Select(ToResponse).ToList();
    }

    // Cars that are in maintenance, active or not. The admin "car active status" page lists these.
    public async Task<List<CarResponse>> GetMaintenanceCarsAsync()
    {
        var cars = await _db.Cars
            .AsNoTracking()
            .Include(c => c.Company)
            .Where(c => c.RentalStatus == RentalStatus.MAINTENANCE)
            .OrderBy(c => c.CarId)
            .ToListAsync();

        return cars.Select(ToResponse).ToList();
    }

    public async Task<MessageResponse> UpdateRentalStatusAsync(UpdateCarRentalStatusRequest request)
    {
        var carId = request.CarId!.Value;

        var car = await _db.Cars.FirstOrDefaultAsync(c => c.CarId == carId)
            ?? throw new AppException("Car not found");

        car.RentalStatus = request.RentalStatus!.Value;
        await _db.SaveChangesAsync();

        return new MessageResponse("Car rental status updated successfully");
    }

    // Used when a returned car is submitted: the car goes to maintenance.
    public async Task<MessageResponse> MarkAsMaintenanceAsync(long carId)
    {
        var car = await _db.Cars.FirstOrDefaultAsync(c => c.CarId == carId)
            ?? throw new AppException("Car not found");

        car.RentalStatus = RentalStatus.MAINTENANCE;
        await _db.SaveChangesAsync();

        return new MessageResponse("Car rental status updated successfully");
    }

    // The React page identifies the car by model name, as it did with Spring.
    public async Task<CarResultResponse> UpdateActiveStatusAsync(UpdateCarActiveStatusRequest request)
    {
        var modelName = request.ModelName.Trim();

        var car = await _db.Cars.Include(c => c.Company).FirstOrDefaultAsync(c => c.ModelName == modelName)
            ?? throw new AppException("Car not found");

        car.IsActive = request.IsActive!.Value;
        await _db.SaveChangesAsync();

        return new CarResultResponse("Car updated successfully", ToResponse(car));
    }

    private static string? CleanTag(string? tag) =>
        string.IsNullOrWhiteSpace(tag) ? null : tag.Trim();

    private static CarResponse ToResponse(Car c) => new(
        c.CarId,
        new CompanyResponse(
            c.Company.CompanyId,
            c.Company.CompanyName,
            c.Company.CompanyEmail,
            c.Company.CompanyPhoneNumber,
            c.Company.CompanyCity,
            c.Company.CompanyAddress,
            c.Company.IsActive),
        c.ModelName,
        c.PricePerHour,
        c.RentalStatus,
        c.Seats,
        c.Fuel,
        c.ImageUrl,
        c.Tag,
        c.IsActive);
}
