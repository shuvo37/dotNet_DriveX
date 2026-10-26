using DriveX.Data;
using DriveX.DTOs.Payments;
using DriveX.Enums;
using DriveX.Exceptions;
using DriveX.Mapping;
using DriveX.Models;
using Microsoft.EntityFrameworkCore;

namespace DriveX.Services;

public class PaymentService : IPaymentService
{
    private readonly DriveXDbContext _db;

    public PaymentService(DriveXDbContext db)
    {
        _db = db;
    }

    // Two kinds of caller, two kinds of payment:
    //  - a customer pays the rental price (RENTED_PRICE) for themselves. The user comes from the token,
    //    never from the request body, so user 5 can not pay or book as user 7.
    //  - ADMIN or STUFF record a late-return fine (FINE) for the customer named in the request.
    public async Task<PaymentResultResponse> CreateAsync(
        CreatePaymentRequest request, long callerUserId, bool callerIsStaff)
    {
        var paymentType = request.PaymentType!.Value;

        long userId;
        if (callerIsStaff)
        {
            if (paymentType != PaymentType.FINE)
            {
                throw new AppException("Staff can only record fine payments");
            }

            userId = request.UserId ?? throw new AppException("userId is required");
        }
        else
        {
            if (paymentType != PaymentType.RENTED_PRICE)
            {
                throw new AppException("Only staff can record a fine payment");
            }

            userId = callerUserId;
        }

        if (!await _db.Users.AnyAsync(u => u.UserId == userId))
        {
            throw new AppException($"User {userId} not found");
        }

        var carId = request.CarId!.Value;

        var car = await _db.Cars.AsNoTracking().Include(c => c.Company)
                      .FirstOrDefaultAsync(c => c.CarId == carId)
                  ?? throw new AppException($"Car {carId} not found");

        // Checked before any payment is saved, so a wrong-city order never leaves an orphan payment.
        if (paymentType == PaymentType.RENTED_PRICE)
        {
            CityRule.EnsureCustomerIsInCarCity(request.City, car.Company.CompanyCity);
        }

        // Stops a customer from paying for a car that can not be booked (saves an orphan payment).
        if (paymentType == PaymentType.RENTED_PRICE &&
            (!car.IsActive || !car.Company.IsActive || car.RentalStatus != RentalStatus.AVAILABLE))
        {
            throw new AppException("Car is not available");
        }

        var payment = new Payment
        {
            CarId = carId,
            UserId = userId,
            PaymentMethod = request.PaymentMethod!.Value,
            PaymentStatus = request.PaymentStatus!.Value,
            PaymentType = paymentType,
            TotalAmount = request.TotalAmount!.Value,
            PaymentTime = DateTime.Now // local time, like Java's LocalDateTime.now()
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        return new PaymentResultResponse("Successfully payment", payment.ToResponse());
    }
}
