using DriveX.DTOs.Companies;
using DriveX.Enums;

namespace DriveX.DTOs.Cars;

// What the API returns for a car. The company is the same CompanyResponse used by the company
// endpoints (React reads car.company.companyName and car.company.companyCity).
// Enums are written as text ("PETROL", "AVAILABLE") by the JSON setting in Program.cs.
public record CarResponse(
    long CarId,
    CompanyResponse Company,
    string ModelName,
    double PricePerHour,
    RentalStatus RentalStatus,
    int Seats,
    FuelType Fuel,
    string? ImageUrl,
    string? Tag,
    bool IsActive);
